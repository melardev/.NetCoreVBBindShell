Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading


Public Class TcpSyncNetService
    Implements INetService

    Private Class ClientData
        Public Property NetworkStream As NetworkStream
        Public Property Reader As StreamReader
        Public Property ClientId As Integer
        Public Property Writer As StreamWriter
        Public Property ClientSocket As Socket
    End Class

    Public Class OutputDataReceivedArgs
        Public Property ClientId As Integer
        Public Property Line As String
    End Class

    Private _serverSocket As Socket
    Public Event OutputDataReceived As LineReceivedHandler
    Public Event ClientAccepted As ClientAcceptedHandler
    Public Event ClientDisconnected As DisconnectionHandler
    Private Property Clients As Dictionary(Of Integer, ClientData) = New Dictionary(Of Integer, ClientData)()

    Public Delegate Sub LineReceivedHandler(ByVal sender As Object, ByVal args As OutputDataReceivedArgs)

    Public Delegate Sub ClientAcceptedHandler(ByVal sender As Object, ByVal clientId As Integer)

    Public Delegate Sub DisconnectionHandler(ByVal sender As Object, ByVal clientId As Integer)

    Public Sub Start(ByVal iPAddress As IPAddress, ByVal port As Integer) Implements INetService.Start
        Dim ipEndPoint As IPEndPoint = New IPEndPoint(iPAddress, 3002)
        _serverSocket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        _serverSocket.Bind(ipEndPoint)
        _serverSocket.Listen(0)
    End Sub


    Public Sub WriteLine(ByVal clientId As Integer, ByVal line As String) Implements INetService.WriteLine
        If Not line.EndsWith(vbLf) Then line += vbLf
        Write(clientId, line)
    End Sub

    Public Sub Write(ByVal clientId As Integer, ByVal output As String) Implements INetService.Write
        Dim clientData As ClientData = Clients(clientId)

        Try
            clientData.Writer.Write(output)
            clientData.Writer.Flush()
        Catch exception As IOException
            CloseAndNotify(clientData)
        End Try
    End Sub


    Public Sub AcceptOneClient() Implements INetService.AcceptOneClient
        Dim clientSocket As Socket = _serverSocket.Accept()
        Dim clientData As ClientData = New ClientData With {
                .ClientId = CInt(clientSocket.Handle),
                .ClientSocket = clientSocket,
                .NetworkStream = New NetworkStream(clientSocket, FileAccess.ReadWrite)
                }
        Clients.Add(CInt(clientSocket.Handle), clientData)
        clientData.Reader = New StreamReader(clientData.NetworkStream)
        clientData.Writer = New StreamWriter(clientData.NetworkStream)
        RaiseEvent ClientAccepted(Me, CInt(clientSocket.Handle))
    End Sub


    Public Sub InteractAsync(ByVal clientId As Integer) Implements INetService.InteractAsync
        Call New Thread(
            Sub()
                Me.ReadSync(clientId)
            End Sub).Start()
    End Sub


    Public Sub ReadSync(ByVal clientId As Integer) Implements INetService.ReadSync
        Dim clientData As ClientData = Me.Clients(clientId)
        Try
            While True
                Dim line As String = clientData.Reader.ReadLine()
                RaiseEvent OutputDataReceived(Me, New OutputDataReceivedArgs With {
                                                 .ClientId = CInt(clientData.ClientSocket.Handle),
                                                 .Line = line
                                                 })
            End While

        Catch exception As IOException
            CloseAndNotify(clientData)
        End Try
    End Sub


    Private Sub CloseAndNotify(ByVal clientData As ClientData)
        Close(clientData)
        RaiseEvent ClientDisconnected(Me, clientData.ClientId)
    End Sub

    Private Sub Close(ByVal clientData As ClientData)
        clientData.Writer.Close()
        clientData.Reader.Close()
        clientData.NetworkStream.Close()
    End Sub

    Public Sub Shutdown() Implements INetService.Shutdown
        _serverSocket.Close()

        For Each clientData As KeyValuePair(Of Integer, ClientData) In Clients
            Close(clientData.Value)
        Next
    End Sub
End Class