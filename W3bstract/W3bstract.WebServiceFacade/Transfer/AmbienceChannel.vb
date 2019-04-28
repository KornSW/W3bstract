Imports System

Public Interface IAmbienceChannel

  Sub ApplyAmbienceDataFromSnapshop(snapshot As AmbienceDataBag)

  Sub AppendAmbienceDataToSnapshop(snapshot As AmbienceDataBag)


End Interface

Public Class AmbienceDataBag

  Public Function GetAmbientData(Of T)() As T



    'TODO



  End Function

  Public Sub SetAmbientData(Of T)(data As T)




    'TODO



  End Sub

End Class