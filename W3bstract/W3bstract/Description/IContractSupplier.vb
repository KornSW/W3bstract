Imports System
Imports System.Collections.Generic
Imports System.Windows.Documents

Namespace ServiceCommunication.Description

  Public Interface IContractSupplier

    Sub DefineContract(contractUrlNode As UrlNodeDescriptor, models As List(Of Type))

  End Interface

End Namespace
