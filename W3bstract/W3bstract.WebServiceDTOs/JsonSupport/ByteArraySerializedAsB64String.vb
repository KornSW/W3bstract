Imports System
Imports Newtonsoft.Json

''' <summary>
''' use with Attribute: JsonConverter(GetType(ByteArraySerializedAsB64String))
''' </summary>
Public Class ByteArraySerializedAsB64String
  Inherits JsonConverter

  Public Overrides Function CanConvert(objectType As Type) As Boolean
    Return (objectType = GetType(Byte()))
  End Function

  Public Overrides Sub WriteJson(writer As JsonWriter, value As Object, serializer As JsonSerializer)
    Dim rawString As String = Convert.ToBase64String(DirectCast(value, Byte()))
    writer.WriteValue(rawString)
  End Sub

  Public Overrides Function ReadJson(reader As JsonReader, objectType As Type, existingValue As Object, serializer As JsonSerializer) As Object
    Dim rawString As String = reader.ReadAsString()
    Dim value As Byte()
    If (String.IsNullOrWhiteSpace(rawString)) Then
      value = {}
    Else
      value = Convert.FromBase64String(rawString)
    End If
    Return value
  End Function

End Class
