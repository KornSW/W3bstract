Imports System
Imports System.Collections.Generic

Public Enum ResponseStatus
  Unknown = 0
  OK = 200
  InternalServerError = 1
  BadRequest = 400
  Forbidden = 403
  Unauthorized = 401
End Enum
