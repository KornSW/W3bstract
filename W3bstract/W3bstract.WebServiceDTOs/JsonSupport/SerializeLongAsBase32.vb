Imports System
Imports System.Runtime.CompilerServices
Imports Newtonsoft.Json

Namespace JsonSupport

  Public Class SerializeLongInQuots
    Inherits JsonConverter

    Public Overrides Sub WriteJson(writer As JsonWriter, value As Object, serializer As JsonSerializer)
      writer.WriteValue(DirectCast(value, Long).ToString())
    End Sub

    Public Overrides Function CanConvert(objectType As Type) As Boolean
      Return (objectType = GetType(Long))
    End Function

    Public Overrides Function ReadJson(reader As JsonReader, objectType As Type, existingValue As Object, serializer As JsonSerializer) As Object
      Dim rawValue As String = DirectCast(reader.Value, String)
      Dim typedValue = DirectCast(existingValue, Long)
      Dim success As Boolean = Long.TryParse(rawValue, typedValue)

      Return typedValue
    End Function

  End Class

  Public Class SerializeLongAsBase32
    Inherits JsonConverter

    Public Overrides Sub WriteJson(writer As JsonWriter, value As Object, serializer As JsonSerializer)

      If (TypeOf value Is Long) Then
        value = LongToBase32String(DirectCast(value, Long))
      End If

      writer.WriteValue(value)
    End Sub

    Public Overrides Function CanConvert(objectType As Type) As Boolean
      Return (objectType = GetType(Long))
    End Function

    Public Overrides Function ReadJson(reader As JsonReader, objectType As Type, existingValue As Object, serializer As JsonSerializer) As Object
      If (objectType = GetType(Long)) Then
        Return Base32StringToLong(DirectCast(reader.Value, String))
      End If
      Return existingValue
    End Function

    Private Shared Function LongToBase32String(extendee As Long) As String
      Return BytesToBase32String(BitConverter.GetBytes(extendee))
    End Function

    Private Shared Function Base32StringToLong(extendee As String) As Long
      Return BitConverter.ToInt64(Base32StringToBytes(extendee), 0)
    End Function

    Private Shared Function Base32StringToBytes(input As String) As Byte()

      If (String.IsNullOrEmpty(input)) Then
        Throw New ArgumentNullException("input")
      End If

      ' input = input.TrimEnd('='); //remove padding characters
      Dim byteCount As Integer = CInt(input.Length * 5 / 8)

      'this must be TRUNCATED
      Dim returnArray As Byte() = New Byte(byteCount) {}

      Dim curByte As Byte = 0
      Dim bitsRemaining As Byte = 8
      Dim mask As Integer = 0
      Dim arrayIndex As Integer = 0

      For Each c As Char In input
        Dim cValue As Integer = CharToValue(c)

        If (bitsRemaining > 5) Then
          mask = cValue << (bitsRemaining - 5)
          curByte = CByte((curByte Or mask))
          bitsRemaining -= CByte(5)
        Else
          mask = cValue >> (5 - bitsRemaining)
          curByte = CByte((curByte Or mask))

          returnArray(arrayIndex) = curByte
          arrayIndex += 1

          curByte = CByte((cValue << (3 + bitsRemaining)) And 255)

          bitsRemaining += CByte(3)
        End If
      Next

      'if we didn't end with a full byte
      If (arrayIndex <> byteCount) Then
        returnArray(arrayIndex) = curByte
      End If

      Return returnArray
    End Function

    Private Shared Function BytesToBase32String(input As Byte()) As String

      If (input Is Nothing OrElse input.Length = 0) Then
        Throw New ArgumentNullException("input")
      End If

      Dim charCount As Integer = CInt(Math.Ceiling(input.Length / 5.0)) * 8
      Dim returnArray As Char() = New Char(charCount) {}

      Dim nextChar As Byte = 0
      Dim bitsRemaining As Byte = 5
      Dim arrayIndex As Integer = 0

      For Each b As Byte In input
        nextChar = CType((nextChar Or (b >> (8 - bitsRemaining))), Byte)
        returnArray(arrayIndex) = ValueToChar(nextChar)
        arrayIndex += 1
        If (bitsRemaining < 4) Then
          nextChar = CType(((b >> (3 - bitsRemaining)) And 31), Byte)
          returnArray(arrayIndex) = ValueToChar(nextChar)
          arrayIndex += 1
          bitsRemaining += CByte(5)
        End If
        bitsRemaining -= CByte(3)
        nextChar = CType(((b << bitsRemaining) And 31), Byte)
      Next

      'if we didn't end with a full char
      If (arrayIndex <> charCount) Then
        returnArray(arrayIndex) = ValueToChar(nextChar)
        arrayIndex += 1
      End If

      Return New String(returnArray, 0, arrayIndex)
    End Function

    Private Shared Function CharToValue(c As Char) As Integer
      Dim value As Integer = Convert.ToInt32(c)

      '65-90 == uppercase letters
      If (value < 91 AndAlso value > 64) Then
        Return value - 65
      End If

      '50-55 == numbers 2-7
      If (value < 56 AndAlso value > 49) Then
        Return value - 24
      End If

      '97-122 == lowercase letters
      If (value < 123 AndAlso value > 96) Then
        Return value - 97
      End If

      Throw New ArgumentException("Character is not a Base32 character.", "c")
    End Function

    Private Shared Function ValueToChar(b As Byte) As Char

      If (b < 26) Then
        Return Convert.ToChar(b + 65)
      End If

      If (b < 32) Then
        Return Convert.ToChar(b + 24)
      End If

      Throw New ArgumentException("Byte is not a value Base32 value.", "b")
    End Function

  End Class

End Namespace
