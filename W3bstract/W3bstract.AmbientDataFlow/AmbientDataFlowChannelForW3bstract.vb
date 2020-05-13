Imports System
Imports System.Collections.Generic
Imports AmbientScoping.DataFlowing
Imports W3bstract
Imports W3bstract.ServiceCommunication

Namespace DataFlowing

  ''' <summary>
  ''' adapter between the 'W3bstract'-Framework and the 'AmbientScoping'-Framework to support
  ''' a transport of ambient states kept in the 'AmbientScoping'-Fx over webservice calls
  ''' </summary>
  Public Class AmbientDataFlowChannelForW3bstract
    Implements IAmbienceChannel

    Public Sub InjectOutgoingData(outgoingDataParams As IList(Of CallParameter)) Implements IAmbienceChannel.InjectOutgoingData
      Dim flowableDataItems As FlowableDataItem() = FlowableDataBuffer.GetInstance().ExtractRawData()
      For Each flowableDataItem As FlowableDataItem In flowableDataItems
        outgoingDataParams.Add(New CallParameter() With {.ParamName = flowableDataItem.FullyQualifiedName, .TypeName = "", .Value = flowableDataItem.FlowableData})
      Next
    End Sub

    Public Sub ProcessIncommingData(incommingDataParams As IList(Of CallParameter)) Implements IAmbienceChannel.ProcessIncommingData
      Dim buffer As FlowableDataBuffer = FlowableDataBuffer.GetInstance()
      For Each incommingDataParam As CallParameter In incommingDataParams
        buffer.SetItem(incommingDataParam.ParamName, incommingDataParam.Value)
      Next
    End Sub

  End Class

End Namespace
