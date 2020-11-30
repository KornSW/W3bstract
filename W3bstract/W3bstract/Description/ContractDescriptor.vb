
Imports System
Imports System.Collections.Generic

Namespace ServiceCommunication.Description

  Public Class ContractDescriptor

    Public Root As New UrlNodeDescriptor

    Public IncludedModels As New List(Of Type)

  End Class

  Public Class UrlNodeDescriptor

    Public SubnodesPerName As New Dictionary(Of String, UrlNodeDescriptor)

    Public OperationsPerVerb As New Dictionary(Of String, OperationDescriptor)

  End Class

  Public Class OperationDescriptor

    Public Property OperationId As String

    Public Property RequestPayloadType As Type
    Public Property ResponsePayloadType As Type

  End Class

End Namespace
