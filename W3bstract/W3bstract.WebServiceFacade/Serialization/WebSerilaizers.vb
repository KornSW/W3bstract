Imports System
Imports System.Collections.Generic
Imports Newtonsoft.Json
Imports W3bstract.ServiceCommunication.JsonSupport

Namespace Serialization

  Friend Interface IWebSerializer

    ReadOnly Property MimeType As String
    Function Serialize(input As Object) As String
    Function Deserialize(input As String, Optional explicitTargetType As Type = Nothing) As Object
    Function Deserialize(Of T)(input As String) As T

  End Interface

  Friend Class WebSerializer
    Implements IWebSerializer

    Private Shared _Instances As New Dictionary(Of String, IWebSerializer)

    Public Shared Function GetInstanceByFormatName(formatName As String) As IWebSerializer
      formatName = formatName.ToLower()
      SyncLock _Instances
        If (_Instances.ContainsKey(formatName)) Then
          Return _Instances(formatName)
        End If
        Dim newInstance As IWebSerializer
        Select Case formatName

          Case "xaml"
            newInstance = New WebSerializer(
            "application/xml",
            Function(obj)
              Return Xaml.Serialize(obj)
            End Function,
            Function(str, tpe)
              If (tpe Is Nothing) Then
                Return Xaml.Deserialize(str)
              Else
                Return Xaml.Deserialize(str, tpe)
              End If

            End Function
          )

          Case "json"
            newInstance = New WebSerializer(
            "application/json",
            Function(obj)
              Dim s = BuildJsonSerializerSettings()
              Return JsonConvert.SerializeObject(obj, s)
            End Function,
            Function(str, tpe)
              Dim s = BuildJsonSerializerSettings()
              If (tpe Is Nothing) Then
                Return JsonConvert.DeserializeObject(str, s)
              Else
                Return JsonConvert.DeserializeObject(str, tpe, s)
              End If
            End Function
          )

          Case Else
            Throw New Exception($"Unknown communication format '{formatName}'")
        End Select
        _Instances.Add(formatName, newInstance)
        Return newInstance
      End SyncLock
    End Function

    Friend Shared Function BuildJsonSerializerSettings() As JsonSerializerSettings

      Dim s As New JsonSerializerSettings
      's.TypeNameHandling = TypeNameHandling.Objects
      's.Formatting = Formatting.Indented

      s.Converters.Add(New SerializeLongInQuots)


      Return s
    End Function

    Private _SerializationMethod As Func(Of Object, String) = Nothing
    Private _DeserializationMethod As Func(Of String, Type, Object) = Nothing

    Public ReadOnly Property MimeType As String Implements IWebSerializer.MimeType

    Private Sub New(mimeType As String, serializationMethod As Func(Of Object, String), deserializationMethod As Func(Of String, Type, Object))
      _SerializationMethod = serializationMethod
      _DeserializationMethod = deserializationMethod
      Me.MimeType = mimeType
    End Sub

    Public Function Serialize(input As Object) As String Implements IWebSerializer.Serialize
      Return _SerializationMethod.Invoke(input)
    End Function

    Public Function Deserialize(input As String, Optional explicitTargetType As Type = Nothing) As Object Implements IWebSerializer.Deserialize
      Return _DeserializationMethod.Invoke(input, explicitTargetType)
    End Function

    Public Function Deserialize(Of T)(input As String) As T Implements IWebSerializer.Deserialize
      Return DirectCast(_DeserializationMethod.Invoke(input, GetType(T)), T)
    End Function

  End Class

End Namespace
