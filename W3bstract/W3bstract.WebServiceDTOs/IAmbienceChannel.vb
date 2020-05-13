Imports System
Imports System.Collections.Generic

Public Interface IAmbienceChannel

  Sub ProcessIncommingData(incommingDataParams As IList(Of CallParameter))

  Sub InjectOutgoingData(outgoingDataParams As IList(Of CallParameter))

End Interface
