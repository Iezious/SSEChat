/**
 * Created by sergey-ki on 12/09/2017.
 */

window.onload = function () {
    
    var btnConnect = document.getElementById("btnConnect");
    var btnSend = document.getElementById("btnSend");
    var tbName = document.getElementById("tbName");
    var tbPhrase = document.getElementById("tbPhrase");
    var divText = document.getElementById("chattext");
    
    var connected = false;
    var socket = null;
    var chatName = null;


    function addMessage(msg)
    {
        var el = document.createElement("div");
        el.innerHTML = msg;
        divText.appendChild(el);
    }
    
    btnConnect.onclick = function () 
    {
        if(connected)
        {
            doDisconnect();
            sendMessage("Disconnected")
        }
        else
        {
            doConnect();
        }
    };
    
    function doDisconnect() {
        if (socket)
        {
            socket = null;
            socket.close();
        }
        btnConnect.disabled = false;
        tbName.disabled = false;
        btnConnect.innerHTML = "Connect";
        connected = false;
    }
    
    function doConnect() 
    {
        chatName = tbName.value;
        btnConnect.disabled = true;
        tbName.disabled = true;
        socket = new EventSource("/sse?name=" + chatName);
        socket.onopen = onConnect;
        socket.onerror = onError;
        socket.onmessage = onMessage;
        connected = true;
    }
    
    function onMessage(e) 
    {
        var msg = JSON.parse(e.data);
        addMessage(msg.Sender + ":" + msg.Message);
    }
    
    function onConnect() 
    {
        btnConnect.disabled = false;
        btnConnect.innerHTML = "Disconnect";
        connected = true;
        addMessage("Connected");

    }
    
    function onError() 
    {
        doDisconnect();
        addMessage("Connection error");
        
    }
    

    function sendMessage(msg)
    {
        var text = tbPhrase.value.trim();
        if (!text) return;

        btnSend.disabled = true;
        
        var xhr = new XMLHttpRequest();
        xhr.open("POST","/sse", true);
        xhr.setRequestHeader("Content-type", "application/json;charset=UTF-8");
        xhr.onreadystatechange = function ()
        {
            if(xhr.readyState == XMLHttpRequest.DONE && xhr.status == 200) 
            {
                tbPhrase.value = "";
                btnSend.disabled = false;
                return;
            }
            
            if (xhr.readyState == XMLHttpRequest.DONE)
            {
                addMessage("Server error");
                btnSend.disabled = false;
                return;
            }
        };
        
        xhr.send(JSON.stringify({Sender: chatName, Message: text}));
    }


    btnSend.onclick = function ()
    {
        if (!connected) return;
        
        sendMessage();

    }
    
    
    
    
    
};



