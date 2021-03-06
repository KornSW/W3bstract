﻿Imports System
Imports System.ComponentModel
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Newtonsoft.Json

Namespace Serialization

  Friend Module JsonTypeConversion

    Private _GenericDeserializeObjectMethod As MethodInfo = (
      GetType(JsonConvert).GetMethods().Where(
          Function(m) m.Name = NameOf(JsonConvert.DeserializeObject) AndAlso
          m.IsGenericMethodDefinition AndAlso m.GetParameters().Count = 2 AndAlso
          m.GetParameters()(1).ParameterType = GetType(JsonSerializerSettings)
      ).Single()
    )

    Public Function MakeTyped(ofType As Type, obj As Object) As Object
      Try
        Dim s = WebSerializer.BuildJsonSerializerSettings()

        Dim untypedCapsle As New JsonTypeConversionCapsle(Of Object) With {.Content = obj}
        Dim genericCapsleType As Type = GetType(JsonTypeConversionCapsle(Of ))
        Dim specificCapsleType As Type = genericCapsleType.MakeGenericType(ofType)
        Dim specificCapsleContentProperty = specificCapsleType.GetProperty("Content")

        Dim serializedString = JsonConvert.SerializeObject(untypedCapsle, specificCapsleType, s)
        Dim specificDeserializeObjectMethod = _GenericDeserializeObjectMethod.MakeGenericMethod(specificCapsleType)

        Dim deserializedCaplseInstance As Object = specificDeserializeObjectMethod.Invoke(Nothing, {serializedString, s})

        obj = specificCapsleContentProperty.GetValue(deserializedCaplseInstance)

        Return obj

      Catch ex As Exception
        Return Nothing
      End Try
    End Function

    Public Function MakeTyped(Of TTarget)(obj As Object) As TTarget
      Try
        Dim s = WebSerializer.BuildJsonSerializerSettings()
        Dim untypedCapsle As New JsonTypeConversionCapsle(Of Object) With {.Content = obj}
        Dim serializedString = JsonConvert.SerializeObject(untypedCapsle)

        Dim typedCapsle = JsonConvert.DeserializeObject(Of JsonTypeConversionCapsle(Of TTarget))(serializedString, s)
        Return typedCapsle.Content
      Catch ex As Exception
        Return Nothing
      End Try
    End Function

  End Module

  Friend Class JsonTypeConversionCapsle(Of T)

    Public Property Content As T

  End Class


End Namespace
