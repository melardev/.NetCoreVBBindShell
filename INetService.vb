Imports System.Net


Public Interface INetService
    Sub Start(ByVal ipAddress As IPAddress, ByVal port As Integer)
    Sub WriteLine(ByVal clientId As Integer, ByVal output As String)
    Sub AcceptOneClient()
    Sub InteractAsync(ByVal clientId As Integer)
    Sub ReadSync(ByVal clientId As Integer)
    Sub Write(ByVal clientId As Integer, ByVal output As String)
    Sub Shutdown()
End Interface