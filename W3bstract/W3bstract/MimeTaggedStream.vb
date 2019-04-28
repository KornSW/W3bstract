Imports System
Imports System.IO

Public Class MimeTaggedStream
  Inherits Stream

  Private _BaseStream As Stream
  Private _MimeType As String

  Public Sub New(baseStream As Stream, mimeType As String)
    _BaseStream = baseStream
    _MimeType = mimeType
  End Sub

  Public ReadOnly Property MimeType As String
    Get
      Return _MimeType
    End Get
  End Property

#Region " Proxy "

  Public Overrides ReadOnly Property CanRead As Boolean
    Get
      Return _BaseStream.CanRead
    End Get
  End Property

  Public Overrides ReadOnly Property CanSeek As Boolean
    Get
      Return _BaseStream.CanSeek
    End Get
  End Property

  Public Overrides ReadOnly Property CanWrite As Boolean
    Get
      Return _BaseStream.CanWrite
    End Get
  End Property

  Public Overrides ReadOnly Property Length As Long
    Get
      Return _BaseStream.Length
    End Get
  End Property

  Public Overrides Property Position As Long
    Get
      Return _BaseStream.Position
    End Get
    Set(value As Long)
      _BaseStream.Position = value
    End Set
  End Property

  Public Overrides Sub Flush()
    _BaseStream.Flush()
  End Sub

  Public Overrides Sub SetLength(value As Long)
    _BaseStream.SetLength(value)
  End Sub

  Public Overrides Sub Write(buffer() As Byte, offset As Integer, count As Integer)
    _BaseStream.Write(buffer, offset, count)
  End Sub

  Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
    Return _BaseStream.Read(buffer, offset, count)
  End Function

  Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
    Return _BaseStream.Seek(offset, origin)
  End Function

#End Region

End Class
