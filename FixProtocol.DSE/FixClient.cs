using QuickFix;
using QuickFix.Transport;
using QuickFix.Fields;
using Microsoft.Extensions.Logging;

namespace FixProtocol.DSE;

/// <summary>
/// FIX Client implementation for DSE-BD (Dhaka Stock Exchange - Bangladesh)
/// Connects to FIX server and sends trading messages
/// </summary>
public class FixClient : IApplication
{
    private readonly ILogger<FixClient> _logger;
    private readonly Dictionary<SessionID, Session> _sessions = new();
    private bool _isLoggedOn = false;

    public FixClient(ILogger<FixClient> logger)
    {
        _logger = logger;
    }

    public bool IsLoggedOn => _isLoggedOn;

    public void OnCreate(SessionID sessionID)
    {
        _logger.LogInformation("Session created: {SessionID}", sessionID);
        _sessions[sessionID] = Session.LookupSession(sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        _logger.LogInformation("Logged on to DSE-BD: {SessionID}", sessionID);
        _logger.LogInformation("  - BeginString: {BeginString}", sessionID.BeginString);
        _logger.LogInformation("  - SenderCompID: {SenderCompID}", sessionID.SenderCompID);
        _logger.LogInformation("  - TargetCompID: {TargetCompID}", sessionID.TargetCompID);
        _isLoggedOn = true;
    }

    public void OnLogout(SessionID sessionID)
    {
        _logger.LogInformation("Logged out from DSE-BD: {SessionID}", sessionID);
        _isLoggedOn = false;
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
        var msgType = new MsgType();
        message.Header.GetField(msgType);
        _logger.LogDebug("Sending admin message: {MessageType}", msgType.getValue());
        LogMessage("ADMIN OUT", message);
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
        var msgType = new MsgType();
        message.Header.GetField(msgType);
        _logger.LogDebug("Received admin message: {MessageType}", msgType.getValue());
        LogMessage("ADMIN IN", message);
    }

    public void ToApp(Message message, SessionID sessionID)
    {
        var msgType = new MsgType();
        message.Header.GetField(msgType);
        _logger.LogInformation("Sending application message: {MessageType}", msgType.getValue());
        LogMessage("APP OUT", message);
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        var msgType = new MsgType();
        message.Header.GetField(msgType);
        _logger.LogInformation("Received application message: {MessageType}", msgType.getValue());
        LogMessage("APP IN", message);
        
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
                case "8": // ExecutionReport
                    _logger.LogInformation("Received Execution Report");
                    ProcessExecutionReport(message);
                    break;
                    
                case "9": // OrderCancelReject
                    _logger.LogInformation("Received Order Cancel Reject");
                    break;
                    
                case "j": // BusinessMessageReject
                    _logger.LogWarning("Received Business Message Reject");
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

    private void ProcessExecutionReport(Message message)
    {
        try
        {
            var execIDField = new ExecID();
            var orderIDField = new OrderID();
            var execTypeField = new ExecType();
            var ordStatusField = new OrdStatus();
            var symbolField = new Symbol();
            var sideField = new Side();
            var leavesQtyField = new LeavesQty();
            var cumQtyField = new CumQty();
            
            message.GetField(execIDField);
            message.GetField(orderIDField);
            message.GetField(execTypeField);
            message.GetField(ordStatusField);
            message.GetField(symbolField);
            message.GetField(sideField);
            message.GetField(leavesQtyField);
            message.GetField(cumQtyField);
            
            var execID = execIDField.getValue();
            var orderID = orderIDField.getValue();
            var execType = execTypeField.getValue().ToString();
            var ordStatus = ordStatusField.getValue().ToString();
            var symbol = symbolField.getValue();
            var side = sideField.getValue().ToString();
            var leavesQty = leavesQtyField.getValue().ToString();
            var cumQty = cumQtyField.getValue().ToString();
            
            _logger.LogInformation("Execution Report Details:");
            _logger.LogInformation("  - ExecID: {ExecID}", execID);
            _logger.LogInformation("  - OrderID: {OrderID}", orderID);
            _logger.LogInformation("  - ExecType: {ExecType}", GetExecTypeDescription(execType));
            _logger.LogInformation("  - OrdStatus: {OrdStatus}", GetOrderStatusDescription(ordStatus));
            _logger.LogInformation("  - Symbol: {Symbol}", symbol);
            _logger.LogInformation("  - Side: {Side}", side == "1" ? "Buy" : "Sell");
            _logger.LogInformation("  - LeavesQty: {LeavesQty}", leavesQty);
            _logger.LogInformation("  - CumQty: {CumQty}", cumQty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing execution report");
        }
    }

    public void SendNewOrder(string symbol, string side, decimal quantity, decimal? price = null)
    {
        if (!_isLoggedOn)
        {
            _logger.LogWarning("Cannot send order - not logged on");
            return;
        }

        try
        {
            var order = new QuickFix.FIX44.NewOrderSingle();
            
            order.SetField(new ClOrdID(Guid.NewGuid().ToString()));
            order.SetField(new Symbol(symbol));
            order.SetField(new Side(side == "BUY" ? '1' : '2'));
            order.SetField(new TransactTime(DateTime.UtcNow));
            order.SetField(new OrderQty(quantity));
            
            if (price.HasValue)
            {
                order.SetField(new OrdType('2')); // Limit
                order.SetField(new Price(price.Value));
            }
            else
            {
                order.SetField(new OrdType('1')); // Market
            }
            
            var sessionID = _sessions.Keys.FirstOrDefault();
            if (sessionID != null && _sessions.TryGetValue(sessionID, out var session))
            {
                session.Send(order);
                _logger.LogInformation("Sent new order: Symbol={Symbol}, Side={Side}, Qty={Quantity}", 
                    symbol, side, quantity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending new order");
        }
    }

    private void LogMessage(string direction, Message message)
    {
        _logger.LogDebug("{Direction}: {Message}", direction, message.ToString());
    }

    private string GetExecTypeDescription(string execType)
    {
        return execType switch
        {
            "0" => "New",
            "1" => "Partial fill",
            "2" => "Fill",
            "4" => "Canceled",
            "8" => "Rejected",
            _ => $"Unknown ({execType})"
        };
    }

    private string GetOrderStatusDescription(string ordStatus)
    {
        return ordStatus switch
        {
            "0" => "New",
            "1" => "Partially filled",
            "2" => "Filled",
            "4" => "Canceled",
            "8" => "Rejected",
            _ => $"Unknown ({ordStatus})"
        };
    }
}
