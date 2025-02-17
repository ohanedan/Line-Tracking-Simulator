﻿using System;
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ControlPackage
{
    public string key;
    public int value;
}

public class AutonomousScript : MonoBehaviour
{
    private static AutonomousScript instance = null;
    public float _verticalVal = 0;
    public float verticalVal
    {
        get
        {
            return _verticalVal;
        }
        set
        {
            if(value > 255) value = 255.0f;
            if(value < 0) value = 0.0f;
            _verticalVal = value;
            if(!isForward)
            {
                if(0 < _verticalVal && _verticalVal < 60)
                {
                    _verticalVal = 60;
                }
                _verticalVal = verticalVal * -1;
            }
        }
    }
    public float _horizontalVal = 0;
    public float horizontalVal
    {
        get
        {
            return _horizontalVal;
        }
        set
        {
            if(value > 254) value = 254.0f;
            _horizontalVal = value - 127;
        }
    }
    public bool pause = false;
    public bool handbrake = false;
    public bool reset = false;
    public bool isForward = true;

    public string socketIp = "127.0.0.1";
    public int socketPort = 6161;
    private TcpListener tcpListener; 
	private Thread tcpListenerThread;
	private TcpClient connectedTcpClient;
    void Start()
    {
        if( instance == null )
        {
            instance = this;
        }
        else if( this != instance )
        {
            Destroy( gameObject );
            return;
        }
        DontDestroyOnLoad(this);
        tcpListenerThread = new Thread (new ThreadStart(ListenForIncommingRequests)); 		
		tcpListenerThread.IsBackground = true;
		tcpListenerThread.Start();
    }

    private void ListenForIncommingRequests () {
        try {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 6161); 			
            tcpListener.Start();
            while(true)
            {
                try {					
                    Byte[] bytes = new Byte[1024];          			
                    while (true) { 				
                        using (connectedTcpClient = tcpListener.AcceptTcpClient()) {
                            SendSocketMessage("ok");
                            using (NetworkStream stream = connectedTcpClient.GetStream()) { 						
                                int length;
                                while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) { 				
                                    var incommingData = new byte[length]; 							
                                    Array.Copy(bytes, 0, incommingData, 0, length);						
                                    string clientMessage = Encoding.ASCII.GetString(incommingData); 							
                                    SendSocketMessage(parseIncomingData(clientMessage));					
                                }	
                            }
                        }
                    }
                } 		
                catch (SocketException ex) { 			
                    Debug.LogWarning(ex.ToString()); 	
                }
                catch(System.IO.IOException ex)
                {
                    Debug.LogWarning(ex.ToString());
                }
            }
        }
        catch (SocketException ex) { 			
            Debug.LogWarning(ex.ToString()); 	
        }
    }

    private void OnDestroy() {
        if(!reset && this == instance)
        {
            tcpListenerThread.Abort();
        }
    }

    private string parseIncomingData(string data)
    {
        try
        {
            ControlPackage package = JsonUtility.FromJson<ControlPackage>(data);
            if(package.key == "vertical")
            {
                verticalVal = package.value;
            }
            else if(package.key == "horizontal")
            {
                horizontalVal = package.value;
            }
            else if(package.key == "pause")
            {
                pause = Convert.ToBoolean(package.value);
            }
            else if(package.key == "handbrake")
            {
                handbrake = Convert.ToBoolean(package.value);
            }
            else if(package.key == "reset")
            {
                if(Convert.ToBoolean(package.value) == true)
                {
                    verticalVal = 0;
                    horizontalVal = 127;
                    pause = false;
                    handbrake = false;
                    isForward = true;
                    reset = true;
                }
            }
            else if(package.key == "isForward")
            {
                isForward = Convert.ToBoolean(package.value);
            }
        }
        catch
        {
            Debug.LogWarning("JSON Parse error.");
        }
        return "ok";
    }

    private void SendSocketMessage(string serverMessage) { 		
		if (connectedTcpClient == null) {             
			return;         
		}  		
		
		try { 					
			NetworkStream stream = connectedTcpClient.GetStream(); 			
			if (stream.CanWrite) {		            
				byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);        
				stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);       
			}       
		} 		
		catch (SocketException ex) {             
			Debug.LogWarning(ex.ToString());
		} 	
	}
}
