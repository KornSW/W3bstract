Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Reflection.Emit

Namespace Dynamics

  'https://www.codeproject.com/Tips/138388/Dynamic-Generation-of-Client-Proxy-at-Runtime-in
  'https://www.codeproject.com/Articles/121568/Dynamic-Type-Using-Reflection-Emit

  Public MustInherit Class DynamicProxy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private WithEvents _Invoker As IDynamicProxyInvoker

    Public Shared Function CreateInstance(Of TApplicable)(ParamArray constructorArgs() As Object) As TApplicable
      Return CreateInstance(Of TApplicable)(New DynamicProxyInvoker, constructorArgs)
    End Function

    Public Shared Function CreateInstance(Of TApplicable)(implementationBuildingMethod As Action(Of DynamicProxyInvoker), ParamArray constructorArgs() As Object) As TApplicable
      Dim invoker As New DynamicProxyInvoker
      implementationBuildingMethod.Invoke(invoker)
      Return CreateInstance(Of TApplicable)(invoker, constructorArgs)
    End Function

    Public Shared Function CreateInstance(applicableType As Type, implementationBuildingMethod As Action(Of DynamicProxyInvoker), ParamArray constructorArgs() As Object) As Object
      Dim invoker As New DynamicProxyInvoker
      implementationBuildingMethod.Invoke(invoker)
      Return CreateInstance(applicableType, invoker, constructorArgs)
    End Function

    Public Shared Function CreateInstance(Of TApplicable)(invoker As IDynamicProxyInvoker, ParamArray constructorArgs() As Object) As TApplicable
      Return DirectCast(CreateInstance(GetType(TApplicable), invoker, constructorArgs), TApplicable)
    End Function

    Public Shared Function CreateInstance(applicableType As Type, invoker As IDynamicProxyInvoker, ParamArray constructorArgs() As Object) As Object
      Dim dynamicType As Type = BuildDynamicType(applicableType)
      Dim extendedConstructorArgs = constructorArgs.ToList()
      extendedConstructorArgs.Add(invoker)
      Dim instance As Object = Activator.CreateInstance(dynamicType, extendedConstructorArgs.ToArray())
      Return instance
    End Function

    Public Shared Function BuildDynamicType(Of TApplicable)() As Type
      Return BuildDynamicType(GetType(TApplicable))
    End Function

    Public Shared Function BuildDynamicType(applicableType As Type) As Type

      'TODO: TYP CACHEN!!!!!!!

      Dim iDynamicProxyInvokerType As Type = GetType(IDynamicProxyInvoker)
      Dim iDynamicProxyInvokerTypeInvokeMethod As MethodInfo = iDynamicProxyInvokerType.GetMethod("InvokeMethod")

      Dim assemblyBuilder As AssemblyBuilder = Nothing
      Dim baseType As Type = Nothing
      If (applicableType.IsClass) Then
        baseType = applicableType
      End If

      '##### ASSEMBLY & MODULE DEFINITION #####

      Dim assemblyName = New AssemblyName(applicableType.Name + "_DyamicProxyClass_Assembly")
      assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave)
      Dim moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTypes", "DynamicTypes.dll")

      '##### CLASS DEFINITION #####

      Dim typeBuilder As TypeBuilder
      If (baseType IsNot Nothing) Then
        typeBuilder = moduleBuilder.DefineType(applicableType.Name + "_DyamicProxyClass", TypeAttributes.Public Or TypeAttributes.Class Or TypeAttributes.AutoClass Or TypeAttributes.AnsiClass Or TypeAttributes.BeforeFieldInit Or TypeAttributes.AutoLayout, baseType)
        'CODE: Public Class <MyApplicableType>_DyamicProxyClass
        '        Inherits <MyApplicableType>
      Else
        typeBuilder = moduleBuilder.DefineType(applicableType.Name + "_DyamicProxyClass", TypeAttributes.Public Or TypeAttributes.Class Or TypeAttributes.AutoClass Or TypeAttributes.AnsiClass Or TypeAttributes.BeforeFieldInit Or TypeAttributes.AutoLayout)
        typeBuilder.AddInterfaceImplementation(applicableType)
        'CODE: Public Class <MyApplicableType>_DyamicProxyClass
        '        Implements <MyApplicableType>
      End If

      '##### FIELD DEFINITIONs #####

      Dim fieldBuilderDynamicProxyInvoker = typeBuilder.DefineField("_DynamicProxyInvoker", iDynamicProxyInvokerType, FieldAttributes.Private)

      '##### CONSTRUCTOR DEFINITIONs #####

      If (baseType IsNot Nothing) Then

        'create a proxy for each constructor in the base class
        For Each constructorOnBase In baseType.GetConstructors()
          Dim constructorArgs As New List(Of Type)
          For Each p In constructorOnBase.GetParameters()
            constructorArgs.Add(p.ParameterType)
          Next
          constructorArgs.Add(GetType(IDynamicProxyInvoker))

          Dim constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public Or MethodAttributes.SpecialName Or
             MethodAttributes.RTSpecialName, CallingConventions.Standard, constructorArgs.ToArray())
          'CODE: Public Sub New([...],dynamicProxyInvoker As IDynamicProxyInvoker)

          ' Dim dynamicProxyInvokerCParam = constructorBuilder.DefineParameter(constructorArgs.Count, ParameterAttributes.In, "dynmaicProxyInvoker")

          With constructorBuilder.GetILGenerator()

            .Emit(OpCodes.Nop) '------------------

            .Emit(OpCodes.Ldarg, 0) 'load Argument(0) (which is a pointer to the instance of our class)
            For i As Integer = 1 To constructorArgs.Count - 1
              .Emit(OpCodes.Ldarg, CByte(i)) 'load the other Arguments (Constructor-Params) excluding the last one
            Next
            .Emit(OpCodes.Call, constructorOnBase) 'CODE: MyBase.New([...])

            .Emit(OpCodes.Nop) '------------------

            .Emit(OpCodes.Ldarg, 0) 'load Argument(0) (which is a pointer to the instance of our class)

            Dim argIndex = CByte(constructorArgs.Count)
            'TODO: prüfen ob valutype!!!!! <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            '.Emit(OpCodes.Ldarg, argIndex) 'load the last Argument (Constructor-Param: IDynamicProxyInvoker)
            .Emit(OpCodes.Ldarg_S, argIndex) 'load the last Argument (Constructor-Param: IDynamicProxyInvoker)
            .Emit(OpCodes.Stfld, fieldBuilderDynamicProxyInvoker) 'CODE: _DynamicProxyInvoker = dynamicProxyInvoker

            .Emit(OpCodes.Nop)
            .Emit(OpCodes.Ret) '------------------

          End With

        Next

      Else 'THIS IS WHEN WERE IMPLEMENTING AN INTERFACE INSTEAD OF INHERITING A CLASS

        Dim constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public Or MethodAttributes.SpecialName Or MethodAttributes.RTSpecialName,
        CallingConventions.HasThis, {GetType(IDynamicProxyInvoker)})

        'CODE: Public Sub New(dynamicProxyInvoker As IDynamicProxyInvoker)

        With constructorBuilder.GetILGenerator()

          .Emit(OpCodes.Nop) '------------------

          .Emit(OpCodes.Ldarg, 0) 'load Argument(0) (which is a pointer to the instance of our class)
          .Emit(OpCodes.Ldarg, 1) 'load the Argument (Constructor-Param: IDynamicProxyInvoker)

          .Emit(OpCodes.Stfld, fieldBuilderDynamicProxyInvoker) 'CODE: _DynamicProxyInvoker = dynamicProxyInvoker

          .Emit(OpCodes.Ret) '------------------

        End With

      End If

      '##### METHOD DEFINITIONs #####

      For Each mi In applicableType.GetMethods()
        Dim methodNameBlacklist As String() = {"ToString", "GetHashCode", "GetType", "Equals"}
        If (Not mi.IsSpecialName AndAlso Not methodNameBlacklist.Contains(mi.Name)) Then

          Dim isOverridable As Boolean = (Not mi.Attributes.HasFlag(MethodAttributes.Final))

          If (mi.IsPublic AndAlso (baseType Is Nothing OrElse isOverridable)) Then

            Dim paramTypes = mi.GetParameters().Select(Function(p) p.ParameterType).ToArray()
            Dim paramNames = mi.GetParameters().Select(Function(p) p.Name).ToArray()
            Dim paramEvalIsValueType = mi.GetParameters().Select(Function(p) p.ParameterType.IsValueType).ToArray()
            Dim paramEvalIsByRef = mi.GetParameters().Select(Function(p) p.IsOut).ToArray()

            Dim methodBuilder = typeBuilder.DefineMethod(mi.Name, MethodAttributes.Public Or MethodAttributes.ReuseSlot Or MethodAttributes.HideBySig Or MethodAttributes.Virtual, mi.ReturnType, paramTypes)
            Dim paramBuilders(paramNames.Length - 1) As ParameterBuilder

            For paramIndex As Integer = 0 To paramNames.Length - 1
              If (paramEvalIsByRef(paramIndex)) Then
                paramBuilders(paramIndex) = methodBuilder.DefineParameter(paramIndex + 1, ParameterAttributes.Out, paramNames(paramIndex))
              Else
                paramBuilders(paramIndex) = methodBuilder.DefineParameter(paramIndex + 1, ParameterAttributes.In, paramNames(paramIndex))
              End If

              'TODO: optionale parameter

            Next

            With methodBuilder.GetILGenerator()

              '##### LOCAL VARIABLE DEFINITIONs #####

              Dim localReturnValue As LocalBuilder = Nothing
              If (mi.ReturnType IsNot Nothing AndAlso Not mi.ReturnType.Name = "Void") Then
                localReturnValue = .DeclareLocal(mi.ReturnType)
              End If

              Dim argumentRedirectionArray = .DeclareLocal(GetType(Object()))
              Dim argumentNameArray = .DeclareLocal(GetType(String()))

              .Emit(OpCodes.Nop) '------------------------------------------------------------------------

              'ARRAY-INSTANZIIEREN
              .Emit(OpCodes.Ldc_I4_S, CByte(paramNames.Length)) ' CODE: Zahl x als (int32) wobei x die anzhalt der parameter unseerer methode ist
              .Emit(OpCodes.Newarr, GetType(Object)) ' CODE: Dim args(x) As Object
              .Emit(OpCodes.Stloc, argumentRedirectionArray)

              .Emit(OpCodes.Nop) '------------------------------------------------------------------------

              'ARRAY-INSTANZIIEREN
              .Emit(OpCodes.Ldc_I4_S, CByte(paramNames.Length)) ' CODE: Zahl x als (int32) wobei x die anzhalt der parameter unseerer methode ist
              .Emit(OpCodes.Newarr, GetType(String)) ' CODE: Dim args(x) As Object
              .Emit(OpCodes.Stloc, argumentNameArray)

              '------------------------------------------------------------------------

              'parameter in transport-array übertragen
              For paramIndex As Integer = 0 To paramNames.Length - 1

                Dim paramIsValueType = paramEvalIsValueType(paramIndex)
                Dim paramIsByRef = paramEvalIsByRef(paramIndex)
                Dim paramType = paramTypes(paramIndex)

                .Emit(OpCodes.Ldloc, argumentRedirectionArray) 'transport-array laden
                .Emit(OpCodes.Ldc_I4_S, CByte(paramIndex)) 'arrayindex als integer (zwecks feld-addressierung) erzeugen

                If (paramIsByRef AndAlso paramIsValueType) Then
                  .Emit(OpCodes.Ldarga_S, paramIndex + 1) 'zuzuweisendes methoden-argument auf den stack holen
                Else
                  .Emit(OpCodes.Ldarg, paramIndex + 1) 'zuzuweisendes methoden-argument auf den stack holen
                End If

                If (paramIsByRef) Then
                  'resolve incomming byref handle into a new object address
                  If (paramIsValueType) Then
                    .Emit(OpCodes.Ldobj, paramType)
                  Else
                    .Emit(OpCodes.Ldind_Ref)
                  End If
                End If
                If (paramIsValueType) Then
                  .Emit(OpCodes.Box, paramType) 'value-types müssen geboxed werden, weil die array-felder vom typ "object" sind
                End If
                .Emit(OpCodes.Stelem_Ref) 'ins transport-array hineinschreiben

                '------------------------------------------------------------------------

                .Emit(OpCodes.Ldloc, argumentNameArray) 'transport-array laden
                .Emit(OpCodes.Ldc_I4_S, CByte(paramIndex)) 'arrayindex als integer (zwecks feld-addressierung) erzeugen
                .Emit(OpCodes.Ldstr, paramNames(paramIndex)) 'name als string bereitlegen (als array inhalt)
                .Emit(OpCodes.Stelem_Ref) 'ins transport-array hineinschreiben

              Next

              .Emit(OpCodes.Ldarg_0) '   < unsere klasseninstanz auf den stack
              .Emit(OpCodes.Ldfld, fieldBuilderDynamicProxyInvoker) ' feld '_DynamicProxyInvoker' laden auf den dtack)
              .Emit(OpCodes.Ldstr, mi.Name) '    < methodenname als string auf den stack holen
              .Emit(OpCodes.Ldloc, argumentRedirectionArray) 'pufferarray auf den stack holen
              .Emit(OpCodes.Ldloc, argumentNameArray) 'pufferarray auf den stack holen

              'aufruf auf umgeleitete funktion absetzen
              .Emit(OpCodes.Callvirt, iDynamicProxyInvokerTypeInvokeMethod) '_DynamicProxyInvoker.InvokeMethod("Foo", args)
              'jetzt liegt ein result auf dem stack...

              If (localReturnValue Is Nothing) Then
                .Emit(OpCodes.Pop) 'result (void) vom stack löschen (weil wir nix zurückgeben)
              Else
                If (mi.ReturnType.IsValueType) Then
                  .Emit(OpCodes.Unbox_Any, mi.ReturnType) 'value-types müssen unboxed werden, weil der retval in "object" ist
                  .Emit(OpCodes.Stloc, localReturnValue) ' < speichere es in 'returnValueBuffer'
                Else
                  .Emit(OpCodes.Castclass, mi.ReturnType) 'reference-types müssen gecastet werden, weil der retval in "object" ist
                  .Emit(OpCodes.Stloc, localReturnValue) ' < speichere es in 'returnValueBuffer'
                End If
              End If

              ''#############################

              'TODO: ByRef-Parameter aus transport-array "auspacken" und zurückschreiben!!!

              '%%%%%%%%%%%%  basearg1 = DirectCast(args(0), String)
              'IL_003c: ldarg.1             < lade das erste methoden-agrument auf den stack

              '  IL_003d: ldloc.1             < lade array auf den stack
              '  IL_003e: ldc.i4.0            < lade integer 0 auf den stack
              '  IL_003f: ldelem.ref          < hole die element-referenz aus dem array heraus

              '  IL_0040: castclass [mscorlib]System.String            < directcast    Bei refernece-types

              'IL_0045: stind.ref           < auf die oben bereitgelegte adresse des byref parameters rückschreiben  OBJECT-VERSION


              '%%%%%%%%%%%%   basearg4 = DirectCast(args(3), Integer)
              'IL_0046: ldarg.s basearg4

              '  IL_0048: ldloc.1             < lade array auf den stack
              '  IL_0049: ldc.i4.3            < lade integer 3 auf den stack
              '  IL_004a: ldelem.ref          < hole die element-referenz aus dem array heraus

              '  IL_004b: unbox.any [mscorlib]System.Int32             < unbox bei value types

              'IL_0050: stind.i4             < auf die bereitgelegte adresse des byref parameters rückschreiben  INTEGER-VERSION

              ''#############################

              If (localReturnValue IsNot Nothing) Then
                .Emit(OpCodes.Ldloc, localReturnValue)
              End If

              .Emit(OpCodes.Ret)
            End With

            'note: 'DefineMethodOverride' is also used for implementing interface-methods
            typeBuilder.DefineMethodOverride(methodBuilder, mi)

          End If
        End If
      Next

      Dim dynamicType = typeBuilder.CreateType()
      'assemblyBuilder.Save("Dynassembly.dll")
      Return dynamicType
    End Function
#Region "..."

    'OverridePropertiesForStandardValues(converterTypeBuilder, baseType)

    '' Methode GetStandardValues mit "Return New StandardValuesCollection(allowedValues)" überschreiben
    'Dim methodBuilder = converterTypeBuilder.DefineMethod("GetStandardValues", MethodAttributes.Public Or MethodAttributes.ReuseSlot Or MethodAttributes.Virtual Or MethodAttributes.HideBySig)
    'methodBuilder.SetReturnType(GetType(TypeConverter.StandardValuesCollection))
    'methodBuilder.SetParameters({GetType(ITypeDescriptorContext)})
    'Dim ilGeneratorOverriding = methodBuilder.GetILGenerator
    'Dim listType = GetType(List(Of String))
    ''lokale Variable anlegen und eine neue Instanz von List(of String) zuweisen
    'ilGeneratorOverriding.DeclareLocal(listType)
    'ilGeneratorOverriding.Emit(OpCodes.Newobj, listType.GetConstructors()(0))
    'ilGeneratorOverriding.Emit(OpCodes.Stloc_0)

    'For index = 0 To allowedValues.Length - 1
    '  'Liste befüllen
    '  ilGeneratorOverriding.Emit(OpCodes.Ldloc_0)
    '  ilGeneratorOverriding.Emit(OpCodes.Ldstr, allowedValues(index))
    '  ilGeneratorOverriding.Emit(OpCodes.Call, listType.GetMethod("Add", {GetType(String)}))
    'Next

    ''Neue Instanz TypeConverter.StandardValuesCollection erzeugen (List(of String) als Konstruktorparameter)
    'ilGeneratorOverriding.Emit(OpCodes.Ldloc_0)
    'ilGeneratorOverriding.Emit(OpCodes.Newobj, GetType(TypeConverter.StandardValuesCollection).GetConstructors()(0))
    ''Und zurück (gibt das oberste Element des Stack zurück.
    'ilGeneratorOverriding.Emit(OpCodes.Ret)

    'Return converterTypeBuilder.CreateType

    'CODDE FÜR REIN:

    'Private _EventTriggers As New Dictionary(Of String, Action(Of Object()))

    'Protected Sub New(invoker As IDynamicProxyInvoker)
    '  _Invoker = invoker
    '  For Each e In Me.GetType().GetEvents()
    '    _EventTriggers.Add(e.Name, Sub(parameters As Object()) e.RaiseMethod.Invoke(Me, parameters))
    '  Next
    'End Sub

    'Public ReadOnly Property Invoker As IDynamicProxyInvoker
    '  Get
    '    Return _Invoker
    '  End Get
    'End Property

    'Protected Function InvokeMethod(methodName As String, arguments As Object()) As Object
    '  Return _Invoker.InvokeMethod(methodName, arguments)
    'End Function

    'Protected Function GetPropertyValue(propertyName As String, indexArguments As Object()) As Object
    '  Return _Invoker.GetPropertyValue(propertyName, indexArguments)
    'End Function

    'Protected Sub SetPropertyValue(propertyName As String, value As Object, indexArguments As Object())
    '  _Invoker.SetPropertyValue(propertyName, value, indexArguments)
    'End Sub

    'Private Sub Invoker_Raising(eventName As String, arguments() As Object) Handles _Invoker.Raise
    '  Dim e As Action(Of Object())
    '  SyncLock _EventTriggers
    '    e = _EventTriggers(eventName)
    '  End SyncLock
    '  e.Invoke(arguments)
    'End Sub

#End Region
  End Class

End Namespace
