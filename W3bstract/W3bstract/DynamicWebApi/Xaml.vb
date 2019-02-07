Imports System
Imports System.IO
Imports System.Text
Imports System.Windows.Markup
Imports System.Xml
'Imports System.Xaml

Public Class Xaml

#Region " String Based Serializer "

  Public Shared Function Serialize(sourceObject As Object) As String

    Dim strBuilder As New StringBuilder
    Dim xmlOutputWriter As XmlWriter

    xmlOutputWriter = XmlWriter.Create(strBuilder, GetXmlSettings())
    XamlWriter.Save(sourceObject, xmlOutputWriter)

    xmlOutputWriter = Nothing

    Return strBuilder.ToString()
  End Function

  Public Shared Sub Serialize(sourceObject As Object, target As Stream)
    Dim strBuilder As New StringBuilder
    Dim xmlOutputWriter As XmlWriter

    xmlOutputWriter = XmlWriter.Create(target, GetXmlSettings())
    XamlWriter.Save(sourceObject, xmlOutputWriter)

    xmlOutputWriter = Nothing
  End Sub

  Public Shared Function Deserialize(Of TargetObjectType)(sourceString As String) As TargetObjectType

    Dim strReader As StringReader
    Dim xmlReader As XmlReader
    Dim targetObject As TargetObjectType

    strReader = New StringReader(sourceString)
    xmlReader = XmlReader.Create(strReader)
    targetObject = DirectCast(XamlReader.Load(xmlReader), TargetObjectType)

    strReader.Dispose()
    strReader = Nothing
    xmlReader = Nothing

    Return targetObject
  End Function

  Public Shared Function Deserialize(sourceString As String, targetObjectType As Type) As Object

    Dim strReader As StringReader
    Dim xmlReader As XmlReader
    Dim targetObject As Object

    strReader = New StringReader(sourceString)
    xmlReader = XmlReader.Create(strReader)
    targetObject = XamlReader.Load(xmlReader)

    If (Not targetObjectType.IsAssignableFrom(targetObject.GetType())) Then
      Throw New Exception($"Deserialized Object Type ('{targetObject.GetType().Name}') does not match to the requested targetObjectType '{targetObjectType.Name}'")
    End If

    strReader.Dispose()
    strReader = Nothing
    xmlReader = Nothing

    Return targetObject
  End Function

  Public Shared Function Deserialize(Of TargetObjectType)(source As Stream) As TargetObjectType

    Dim xmlReader As XmlReader
    Dim targetObject As TargetObjectType

    xmlReader = XmlReader.Create(source)
    targetObject = DirectCast(XamlReader.Load(xmlReader), TargetObjectType)

    xmlReader = Nothing

    Return targetObject
  End Function

#End Region

#Region " Xaml Format Settings "

  Protected Shared xmlSettings As XmlWriterSettings = Nothing

  Protected Shared Function GetXmlSettings() As XmlWriterSettings

    If (xmlSettings Is Nothing) Then

      xmlSettings = New XmlWriterSettings

      xmlSettings.Indent = True
      xmlSettings.NewLineChars = Environment.NewLine
      xmlSettings.NewLineOnAttributes = False
      xmlSettings.NewLineHandling = NewLineHandling.Entitize
      xmlSettings.CloseOutput = True

    End If

    Return xmlSettings
  End Function

#End Region

End Class
