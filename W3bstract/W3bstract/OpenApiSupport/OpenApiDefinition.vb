Imports System.IO
Imports System.Reflection
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Collections.Generic
Imports System
Imports System.Runtime.CompilerServices

Namespace ServiceCommunication.OpenApiSupport

  Friend Module Module1

    Sub Main()
      Const inputPath As String = "D:\(git)\cslsPlatform-Frontends\Platform\MemberApi.MVC\legacy-api-swagger.json"

      Dim rawDefinition As String = File.ReadAllText(inputPath, Encoding.UTF8)

      Dim definition As OpenApiDefinition = OpenApiDefinition.FromJson(rawDefinition)

      Dim outputBuffer As New StringBuilder
      Dim outputWriter As New StringWriter(outputBuffer)

      VbGenerator.GenerateMethodInterfacesByTag(definition, outputWriter, "Mt2s.LegacyApi")

      Dim output As String = outputBuffer.ToString()

      Console.WriteLine(output)
      Console.ReadLine()
    End Sub

#Region " Helper Extensios "

    <Extension>
    Public Sub TryReadPropertyValue(Of T)(extendee As JObject, propertyName As String, callback As Action(Of T))
      extendee.TryGetProperty(propertyName, Sub(p) p.TryGetValue(Of T)(callback))
    End Sub

    <Extension>
    Public Sub TryGetProperty(extendee As JObject, propertyName As String, callback As Action(Of JProperty))
      Dim prop = extendee.Property(propertyName)
      If (prop IsNot Nothing) Then
        callback.Invoke(prop)
      End If
    End Sub

    <Extension>
    Public Sub TryGetPropertyAsJObject(extendee As JObject, propertyName As String, callback As Action(Of JObject))
      Dim prop = extendee.Property(propertyName)
      If (prop IsNot Nothing) Then
        callback.Invoke(DirectCast(prop.Value, JObject))
      End If
    End Sub

    <Extension>
    Public Sub TryGetPropertyAsDictionary(extendee As JObject, propertyName As String, callback As Action(Of Dictionary(Of String, JProperty)))
      extendee.TryGetPropertyAsJObject(
      propertyName,
      Sub(child)
        Dim dict As New Dictionary(Of String, JProperty)
        For Each p In child.Properties
          dict.Add(p.Name, p)
        Next
        callback.Invoke(dict)
      End Sub
    )
    End Sub

    <Extension>
    Public Function ToObjectDictionary(extendee As JObject) As Dictionary(Of String, JObject)
      Dim dict As New Dictionary(Of String, JObject)
      For Each p In extendee.Properties
        dict.Add(p.Name, p.GetValueObject())
      Next
      Return dict
    End Function

    <Extension>
    Public Sub TryGetPropertyAsObjectDictionary(extendee As JObject, propertyName As String, callback As Action(Of Dictionary(Of String, JObject)))
      extendee.TryGetPropertyAsJObject(
      propertyName,
      Sub(child)
        callback.Invoke(child.ToObjectDictionary())
      End Sub
    )
    End Sub

    <Extension>
    Public Sub TryGetValue(Of T)(extendee As JProperty, callback As Action(Of T))
      Dim obj As T = extendee.Value.ToObject(Of T)()
      callback.Invoke(obj)
    End Sub

    <Extension>
    Public Function GetValueObject(extendee As JProperty) As JObject
      Return DirectCast(extendee.Value, JObject)
    End Function

    <Extension>
    Public Function GetValueObjectArray(extendee As JProperty) As JObject()
      Return extendee.Values.OfType(Of JObject).ToArray()
    End Function

#End Region

  End Module

  Public Class VbGenerator

    Private Class WritingContext
      Public Property IntendLevel As Integer = 0
      Public Property Writer As TextWriter
      Public Sub WriteLine(Optional intend As Boolean = False)
        If (intend) Then
          Me.Writer.WriteLine(New String(" "c, IntendLevel * 2))
        Else
          Me.Writer.WriteLine()
        End If
      End Sub

      Public Sub Write(content As String, Optional intend As Boolean = False)
        If (intend) Then
          Me.Writer.Write(New String(" "c, IntendLevel * 2))
        End If
        Me.Writer.Write(content)
      End Sub

      Public Sub WriteLine(content As String, Optional intend As Boolean = True)
        If (intend) Then
          Me.Writer.Write(New String(" "c, IntendLevel * 2))
        End If
        Me.Writer.WriteLine(content)
      End Sub

      Public Sub PushIntend()
        Me.IntendLevel += 1
      End Sub

      Public Sub PopIntend()
        Me.IntendLevel -= 1
      End Sub

    End Class

    Private Shared Function GetSchemaTypeName(schema As OpenApiSchemaDeclaration) As String
      If (schema Is Nothing) Then
        Return "Object"

      ElseIf (Not String.IsNullOrWhiteSpace(schema.Ref)) Then
        Dim schemaLocationTokens = schema.Ref.Split("/").ToArray()
        If (schemaLocationTokens.Length = 4) Then
          Return FirstToUpper(schemaLocationTokens(3))
        Else
          Return "Object"
        End If
      Else

        Select Case schema.Type
          Case "array" : Return GetSchemaTypeName(schema.Items) + "()"

          Case "object" : Return "Object"
          'hier müsste eigentlich n anonymer typ entstehen
          Case "string" : Return "String"

          Case "boolean" : Return "Boolean"

          Case "integer" : Return "Integer"

        End Select

        If (String.IsNullOrWhiteSpace(schema.Type)) Then
          Return "Object"
        Else
          Return FirstToUpper(schema.Type)
        End If

      End If

    End Function


    ''' <summary> </summary>
    ''' <param name="source"></param>
    ''' <param name="target"></param>
    ''' <param name="rootNs"></param>
    Public Shared Sub GenerateMethodInterfacesByTag(source As OpenApiDefinition, target As TextWriter, rootNs As String)
      Dim ctx As New WritingContext With {.Writer = target}

      If (rootNs IsNot Nothing) Then
        ctx.WriteLine("Namespace " + rootNs)
        ctx.PushIntend()
      End If

      'this is to be sorted alphanumerically per TAG and OperationId
      Dim writingBatch As New Dictionary(Of String, Dictionary(Of String, Action))

      For Each path In source.Paths.Keys
        Dim pathDetails = source.Paths(path)
        Dim pathTokens = path.Split("/")

        For Each httpVerb In pathDetails.HttpMethods.Keys
          Dim method = pathDetails.HttpMethods(httpVerb)

          If (String.IsNullOrWhiteSpace(method.OperationId)) Then

            method.OperationId = httpVerb
            For Each token In pathTokens
              If (Not token.Contains("{")) Then
                method.OperationId = method.OperationId + FirstToUpper(token)
              End If
            Next

          End If

          If (method.Tags IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(method.OperationId)) Then

            Dim writingMethod As Action = (
            Sub()
              ctx.WriteLine()

              If (Not String.IsNullOrWhiteSpace(method.Summary)) Then
                ctx.WriteLine($"''' <summary> {method.Summary.Trim()} </summary>")
              End If

              ctx.Write("Sub " + FirstToUpper(method.OperationId) + "(", True)

              If (method.Parameters IsNot Nothing) Then
                Dim first As Boolean = True
                For Each p In method.Parameters
                  If (first) Then
                    first = False
                  Else
                    ctx.Write(", ")
                  End If
                  ctx.Write(FirstToLower(p.Name))
                  ctx.Write(" As ")


                  Dim typeNAme As String = GetSchemaTypeName(p.Schema)
                  ctx.Write(typeNAme)





                Next
              End If

              ctx.WriteLine(")", False)

            End Sub
          )

            For Each tag In method.Tags
              Dim targetBatch As Dictionary(Of String, Action)
              If (writingBatch.ContainsKey(tag)) Then
                targetBatch = writingBatch(tag)
              Else
                targetBatch = New Dictionary(Of String, Action)
                writingBatch.Add(tag, targetBatch)
              End If
              targetBatch(method.OperationId) = writingMethod
            Next

          End If
        Next
      Next

      For Each tagName In writingBatch.Keys

        ctx.WriteLine()
        ctx.WriteLine("Public Interface I" + FirstToUpper(tagName) + "Operations")
        ctx.PushIntend()

        Dim opsPerTag = writingBatch(tagName)
        For Each operationId In opsPerTag.Keys
          opsPerTag(operationId).Invoke()
        Next


        ctx.WriteLine()
        ctx.PopIntend()
        ctx.WriteLine("End Interface ")
      Next

      If (rootNs IsNot Nothing) Then
        ctx.WriteLine()
        ctx.PopIntend()
        ctx.WriteLine("End Namespace ")
      End If

    End Sub

    Private Shared Function FirstToUpper(input As String) As String
      If (input.Length > 0 AndAlso Char.IsLower(input(0))) Then
        input = Char.ToUpper(input(0)).ToString() + input.Substring(1)
      End If
      Return input
    End Function

    Private Shared Function FirstToLower(input As String) As String
      If (input.Length > 0 AndAlso Char.IsUpper(input(0))) Then
        input = Char.ToLower(input(0)).ToString() + input.Substring(1)
      End If
      Return input
    End Function
  End Class

#Region " DOM "

  <DebuggerDisplay("OpenApiDefinition {Info}")>
  Public Class OpenApiDefinition

    Public Shared Function FromJson(jsonString As String) As OpenApiDefinition
      Dim rawObject = DirectCast(JsonConvert.DeserializeObject(jsonString), JObject)
      Dim domObj As New OpenApiDefinition
      MapJObjectToOpenApiDefinition(rawObject, domObj)
      Return domObj
    End Function

    Friend Shared Sub MapJObjectToOpenApiDefinition(source As JObject, target As OpenApiDefinition)

      source.TryReadPropertyValue(Of String)("openapi", Sub(value) target.OpenApiVersion = value)

      source.TryGetPropertyAsJObject("info",
      Sub(childObj)
        target.Info = New OpenApiInfoBlock
        OpenApiInfoBlock.MapJObjectToDom(childObj, target.Info)
      End Sub
    )

      source.TryGetPropertyAsObjectDictionary("paths",
      Sub(childProps)
        target.Paths = New Dictionary(Of String, OpenApiPathDirective)
        For Each path As String In childProps.Keys
          Dim pathDirective As New OpenApiPathDirective
          OpenApiPathDirective.MapJObjectToDom(childProps(path), pathDirective)
          target.Paths.Add(path, pathDirective)
        Next
      End Sub
    )

      source.TryGetPropertyAsJObject("components",
      Sub(childObj)
        target.Components = New OpenApiComponentsDirective
        OpenApiComponentsDirective.MapJObjectToDom(childObj, target.Components)
      End Sub
    )

    End Sub

    Public Property OpenApiVersion As String
    Public Property Info As OpenApiInfoBlock

    ''' <summary> (by Path starting with "/...") </summary>
    Public Property Paths As Dictionary(Of String, OpenApiPathDirective)
    Public Property Components As OpenApiComponentsDirective

  End Class

  Public Class OpenApiComponentsDirective

    ''' <summary> (by Name) </summary>
    Public Property Schemas As Dictionary(Of String, OpenApiSchemaDeclaration)

    Friend Shared Sub MapJObjectToDom(source As JObject, target As OpenApiComponentsDirective)

      source.TryGetPropertyAsObjectDictionary("schemas",
      Sub(childProps)
        target.Schemas = New Dictionary(Of String, OpenApiSchemaDeclaration)
        For Each path As String In childProps.Keys
          Dim item As New OpenApiSchemaDeclaration
          OpenApiSchemaDeclaration.MapJObjectToDom(childProps(path), item)
          target.Schemas.Add(path, item)
        Next
      End Sub
    )
    End Sub

  End Class

  <DebuggerDisplay("{Title}")>
  Public Class OpenApiInfoBlock

    Friend Shared Sub MapJObjectToDom(source As JObject, target As OpenApiInfoBlock)

      source.TryReadPropertyValue(Of String)("title", Sub(value) target.Title = value)
      source.TryReadPropertyValue(Of String)("description", Sub(value) target.Description = value)
      source.TryReadPropertyValue(Of String)("version", Sub(value) target.Version = value)

    End Sub

    Public Property Title As String
    Public Property Description As String
    Public Property Version As String

    'Contact
  End Class

  Public Class OpenApiPathDirective

    Friend Shared Sub MapJObjectToDom(source As JObject, target As OpenApiPathDirective)
      Dim props = source.ToObjectDictionary()
      target.HttpMethods = New Dictionary(Of String, OpenApiMethodDirective)
      For Each path As String In props.Keys
        Dim item As New OpenApiMethodDirective
        OpenApiMethodDirective.MapJObjectToDom(props(path), item)
        target.HttpMethods.Add(path, item)
      Next
    End Sub

    ''' <summary> (by HTTP-Verb) </summary>
    Public Property HttpMethods() As Dictionary(Of String, OpenApiMethodDirective)

  End Class

  Public Class OpenApiMethodDirective

    Friend Shared Sub MapJObjectToDom(source As JObject, target As OpenApiMethodDirective)

      source.TryReadPropertyValue(Of String())("tags", Sub(value) target.Tags = value.ToList())
      source.TryReadPropertyValue(Of String)("operationId", Sub(value) target.OperationId = value)
      source.TryReadPropertyValue(Of String)("summary", Sub(value) target.Summary = value)

      source.TryGetProperty(
      "parameters",
      Sub(p)
        target.Parameters = New List(Of OpenApiMethodParameter)
        For Each item In p.GetValueObjectArray()
          Dim child As New OpenApiMethodParameter
          OpenApiMethodParameter.MapJObjectToDom(item, child)
          target.Parameters.Add(child)
        Next
      End Sub
    )

      source.TryGetPropertyAsJObject("requestBody",
      Sub(childObj)
        target.RequestBody = New OpenApiMethodRequestBodyDirective
        OpenApiMethodRequestBodyDirective.MapJObjectToDom(childObj, target.RequestBody)
      End Sub
    )

      source.TryGetPropertyAsObjectDictionary("responses",
      Sub(childProps)
        target.Responses = New Dictionary(Of Integer, OpenApiMethodResponseDirective)
        For Each code As String In childProps.Keys
          Dim item As New OpenApiMethodResponseDirective
          OpenApiMethodResponseDirective.MapJObjectToDom(childProps(code), item)
          target.Responses.Add(Integer.Parse(code), item)
        Next
      End Sub
    )

    End Sub

    Public Property Tags() As List(Of String)
    Public Property OperationId As String
    Public Property Summary As String

    Public Property Parameters As List(Of OpenApiMethodParameter)

    Public Property RequestBody As OpenApiMethodRequestBodyDirective

    ''' <summary> (by HTTP-Response-Code) </summary>
    Public Property Responses As Dictionary(Of Integer, OpenApiMethodResponseDirective)

  End Class

  Public Class OpenApiMethodParameter

    Friend Shared Sub MapJObjectToDom(source As JObject, target As OpenApiMethodParameter)

      source.TryReadPropertyValue(Of String)("name", Sub(value) target.Name = value)

      source.TryReadPropertyValue(Of String)("in",
      Sub(value)
        If ([Enum].TryParse(Of OpenApiMethodParameterSource)(value, target.In) = False) Then
          target.In = OpenApiMethodParameterSource.Undefined
        End If
      End Sub
    )

      source.TryReadPropertyValue(Of String)("description", Sub(value) target.Description = value)
      source.TryReadPropertyValue(Of Boolean)("required", Sub(value) target.Required = value)

      source.TryGetPropertyAsJObject("schema",
      Sub(childObj)
        target.Schema = New OpenApiSchemaDeclaration
        OpenApiSchemaDeclaration.MapJObjectToDom(childObj, target.Schema)
      End Sub
    )

    End Sub

    Public Property Name As String
    Public Property [In] As OpenApiMethodParameterSource
    Public Property Description As String
    Public Property Required As Boolean
    Public Property Schema As OpenApiSchemaDeclaration

  End Class

  Public Enum OpenApiMethodParameterSource
    Undefined = 0
    Path = 1
    Query = 2
    Header = 3
  End Enum

  Public Class OpenApiMethodRequestBodyDirective

    Friend Shared Sub MapJObjectToDom(source As JObject, target As OpenApiMethodRequestBodyDirective)
      source.TryReadPropertyValue(Of String)("description", Sub(value) target.Description = value)

      source.TryGetPropertyAsObjectDictionary("content",
      Sub(childProps)
        target.Content = New Dictionary(Of String, OpenApiContentDirective)
        For Each path As String In childProps.Keys
          Dim item As New OpenApiContentDirective
          OpenApiContentDirective.MapJObjectToDom(childProps(path), item)
          target.Content.Add(path, item)
        Next
      End Sub
    )

    End Sub

    Public Property Description As String

    ''' <summary> (by supported MimeType) </summary>
    Public Property Content As Dictionary(Of String, OpenApiContentDirective)
  End Class

  Public Class OpenApiContentDirective

    Friend Shared Sub MapJObjectToDom(source As JObject, target As OpenApiContentDirective)

      source.TryReadPropertyValue(Of String)("description", Sub(value) target.Description = value)

      source.TryGetPropertyAsJObject("schema",
      Sub(childObj)
        target.Schema = New OpenApiSchemaDeclaration
        OpenApiSchemaDeclaration.MapJObjectToDom(childObj, target.Schema)
      End Sub
    )

    End Sub

    Public Property Description As String
    Public Property Schema As OpenApiSchemaDeclaration

  End Class

  Public Class OpenApiSchemaDeclaration

    Friend Shared Sub MapJObjectToDom(source As JObject, target As OpenApiSchemaDeclaration)



      source.TryReadPropertyValue(Of String)("type", Sub(value) target.Type = value)

      source.TryReadPropertyValue(Of String)("$ref", Sub(value) target.Ref = value)

      source.TryReadPropertyValue(Of String)("format", Sub(value) target.Format = value)

      source.TryGetPropertyAsJObject("items",
      Sub(childObj)
        target.Items = New OpenApiSchemaDeclaration
        OpenApiSchemaDeclaration.MapJObjectToDom(childObj, target.Items)
      End Sub
    )

      source.TryGetPropertyAsObjectDictionary("properties",
      Sub(childProps)
        target.Properties = New Dictionary(Of String, OpenApiSchemaDeclaration)
        For Each path As String In childProps.Keys
          Dim item As New OpenApiSchemaDeclaration
          OpenApiSchemaDeclaration.MapJObjectToDom(childProps(path), item)
          target.Properties.Add(path, item)
        Next
      End Sub
    )

      source.TryReadPropertyValue(Of Boolean)("additionalProperties", Sub(value) target.AdditionalProperties = value)
      source.TryReadPropertyValue(Of String)("description", Sub(value) target.Description = value)
      source.TryReadPropertyValue(Of Boolean)("nullable", Sub(value) target.Nullable = value)
      source.TryReadPropertyValue(Of Boolean)("deprecated", Sub(value) target.Deprecated = value)

    End Sub

    Public Property Type As String

    ''' <summary>if Type=null</summary>
    Public Property Ref As String

    ''' <summary>if Type="integer|string|..."</summary>
    Public Property Format As String

    ''' <summary>if Type="array"</summary>
    Public Property Items As OpenApiSchemaDeclaration

    ''' <summary>if Type="object"</summary>
    Public Property Properties As Dictionary(Of String, OpenApiSchemaDeclaration)

    ''' <summary>if Type="object"</summary>
    Public Property AdditionalProperties As Boolean

    Public Property Description As String
    Public Property Nullable As Boolean
    Public Property Deprecated As Boolean

  End Class

  Public Class OpenApiMethodResponseDirective

    Friend Shared Sub MapJObjectToDom(source As JObject, target As OpenApiMethodResponseDirective)

      source.TryReadPropertyValue(Of String)("description", Sub(value) target.Description = value)

      source.TryGetPropertyAsObjectDictionary("content",
      Sub(childProps)
        target.Content = New Dictionary(Of String, OpenApiContentDirective)
        For Each path As String In childProps.Keys
          Dim item As New OpenApiContentDirective
          OpenApiContentDirective.MapJObjectToDom(childProps(path), item)
          target.Content.Add(path, item)
        Next
      End Sub
    )

    End Sub

    Public Property Description As String

    ''' <summary> (by supported MimeType) </summary>
    Public Property Content As Dictionary(Of String, OpenApiContentDirective)

  End Class

#End Region

End Namespace
