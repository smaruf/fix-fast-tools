using QuickFix;
using QuickFix.Transport;
using QuickFix.Fields;
using Microsoft.Extensions.Logging;

namespace FixProtocol.CSE;

/// <summary>
/// FIX Server implementation for CSE-BD (Chittagong Stock Exchange - Bangladesh)
/// Accepts FIX connections and handles trading messages with proper session management
/// </summary>
public class FixServer : IApplication
{
    private readonly ILogger<FixServer> _logger;
    private readonly Dictionary<SessionID, Session> _sessions = new();

    public FixServer(ILogger<FixServer> logger)
    {
        _logger = logger;
    }

    public void OnCreate(SessionID sessionID)
    {
        _logger.LogInformation("Session created: {SessionID}", sessionID);
        _sessions[sessionID] = Session.LookupSession(sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        _logger.LogInformation("Client logged on: {SessionID}", sessionID);
        _logger.LogInformation("  - BeginString: {BeginString}", sessionID.BeginString);
        _logger.LogInformation("  - SenderCompID: {SenderCompID}", sessionID.SenderCompID);
        _logger.LogInformation("  - TargetCompID: {TargetCompID}", sessionID.TargetCompID);
    }

    public void OnLogout(SessionID sessionID)
    {
        _logger.LogInformation("Client logged out: {SessionID}", sessionID);
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
        var msgType = new MsgType();
        message.Header.GetField(msgType);
        _logger.LogDebug("Sending admin message to {SessionID}: {MessageType}", 
            sessionID, msgType.getValue());
        LogMessage("ADMIN OUT", message);
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
        var msgType = new MsgType();
        message.Header.GetField(msgType);
        _logger.LogDebug("Received admin message from {SessionID}: {MessageType}", 
            sessionID, msgType.getValue());
        LogMessage("ADMIN IN", message);
    }

    public void ToApp(Message message, SessionID sessionID)
    {
        var msgType = new MsgType();
        message.Header.GetField(msgType);
        _logger.LogInformation("Sending application message to {SessionID}: {MessageType}", 
            sessionID, msgType.getValue());
        LogMessage("APP OUT", message);
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        var msgType = new MsgType();
        message.Header.GetField(msgType);
        _logger.LogInformation("Received application message from {SessionID}: {MessageType}", 
            sessionID, msgType.getValue());
        LogMessage("APP IN", message);
        
        // Process the message based on type
        ProcessMessage(message, sessionID);
    }

    private void ProcessMessage(Message message, SessionID sessionID)
    {
        try
        {
            var msgTypeField = new MsgType();
            message.Header.GetField(msgTypeField);
            var msgType = msgTypeField.getValue();
            
            switch (msgType)
            {
                case "D": // NewOrderSingle
                    _logger.LogInformation("Processing New Order Single");
                    ProcessNewOrder(message, sessionID);
                    break;
                    
                case "G": // OrderCancelRequest
                    _logger.LogInformation("Processing Order Cancel Request");
                    break;
                    
                case "F": // OrderCancelReplaceRequest
                    _logger.LogInformation("Processing Order Cancel/Replace Request");
                    break;
                    
                case "H": // OrderStatusRequest
                    _logger.LogInformation("Processing Order Status Request");
                    break;
                    
                default:
                    _logger.LogWarning("Unknown message type: {MsgType}", msgType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
        }
    }

    private void ProcessNewOrder(Message message, SessionID sessionID)
    {
        try
        {
            var clOrdIDField = new ClOrdID();
            var symbolField = new Symbol();
            var sideField = new Side();
            var orderQtyField = new OrderQty();
            var ordTypeField = new OrdType();
            
            message.GetField(clOrdIDField);
            message.GetField(symbolField);
            message.GetField(sideField);
            message.GetField(orderQtyField);
            message.GetField(ordTypeField);
            
            var clOrdID = clOrdIDField.getValue();
            var symbol = symbolField.getValue();
            var side = sideField.getValue().ToString();
            var orderQty = orderQtyField.getValue().ToString();
            var ordType = ordTypeField.getValue().ToString();
            
            _logger.LogInformation("Order Details:");
            _logger.LogInformation("  - ClOrdID: {ClOrdID}", clOrdID);
            _logger.LogInformation("  - Symbol: {Symbol}", symbol);
            _logger.LogInformation("  - Side: {Side}", side == "1" ? "Buy" : "Sell");
            _logger.LogInformation("  - Quantity: {OrderQty}", orderQty);
            _logger.LogInformation("  - Order Type: {OrdType}", ordType);
            
            if (message.IsSetField(Tags.Price))
            {
                var priceField = new Price();
                message.GetField(priceField);
                var price = priceField.getValue().ToString();
                _logger.LogInformation("  - Price: {Price}", price);
            }
            
            // Send execution report (acknowledgement)
            SendExecutionReport(message, sessionID, '0'); // '0' = New
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing new order");
        }
    }

    private void SendExecutionReport(Message orderMessage, SessionID sessionID, char execType)
    {
        try
        {
            var execReport = new QuickFix.FIX44.ExecutionReport();
            
            var symbolField = new Symbol();
            var sideField = new Side();
            var orderQtyField = new OrderQty();
            
            orderMessage.GetField(symbolField);
            orderMessage.GetField(sideField);
            orderMessage.GetField(orderQtyField);
            
            execReport.SetField(new OrderID(Guid.NewGuid().ToString()));
            execReport.SetField(new ExecID(Guid.NewGuid().ToString()));
            execReport.SetField(new ExecType(execType));
            execReport.SetField(new OrdStatus(execType));
            execReport.SetField(new Symbol(symbolField.getValue()));
            execReport.SetField(new Side(sideField.getValue()));
            execReport.SetField(new LeavesQty(orderQtyField.getValue()));
            execReport.SetField(new CumQty(0));
            execReport.SetField(new AvgPx(0));
            
            if (_sessions.TryGetValue(sessionID, out var session))
            {
                session.Send(execReport);
                _logger.LogInformation("Sent execution report: {ExecID}", execReport.ExecID.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending execution report");
        }
    }

    private void LogMessage(string direction, Message message)
    {
        _logger.LogDebug("{Direction}: {Message}", direction, message.ToString());
    }
}
