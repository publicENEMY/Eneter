using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using NLog;

namespace WpfServer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ObservableCollection<string> _messages = new ObservableCollection<string>();
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private const string TargetName = "memoryex";

		private TcpPolicyServer myPolicyServer;
		private IDuplexStringMessageReceiver myMessageReceiver;

		public MainWindow()
		{
			InitializeComponent();
			Loaded += MainWindowLoaded;
			SizeChanged += MainWindowSizeChanged;

			var target =
				LogManager.Configuration.AllTargets
				.Where(x => x.Name == TargetName)
				.Single() as MemoryTargetEx;

			if (target != null)
				target.Messages.Subscribe(msg => _messages.Add(msg));

			// Start the policy server to be able to communicate with silverlight.
			// Note: Before Silverlight open the communication it asks the policy
			//       server for the policy xml.
			//       If the policy server is not present or the content of the
			//       policy xml does not allow the communication the communication
			//       is not open.
			myPolicyServer = new TcpPolicyServer();
			myPolicyServer.StartPolicyServer();

			// Create duplex message receiver.
			// It can receive messages and also send back response messages.
			IDuplexStringMessagesFactory aStringMessagesFactory = new DuplexStringMessagesFactory();
			myMessageReceiver = aStringMessagesFactory.CreateDuplexStringMessageReceiver();
			myMessageReceiver.ResponseReceiverConnected += ClientConnected;
			myMessageReceiver.ResponseReceiverDisconnected += ClientDisconnected;
			myMessageReceiver.RequestReceived += MessageReceived;
			 
			// Create TCP based messaging.
			// Note: TCP in Silverlight can use only ports 4502 - 4532.
			IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
			IDuplexInputChannel aDuplexInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:4502/");

			// Attach the duplex input channel to the message receiver and start listening.
			// Note: Duplex input channel can receive messages but also send messages back.
			myMessageReceiver.AttachDuplexInputChannel(aDuplexInputChannel);
		}

		static void MainWindowSizeChanged(object sender, SizeChangedEventArgs e)
		{
			Logger.Info("Window Size Changed; New size: {0}, {1}", e.NewSize.Height, e.NewSize.Width);
		}

		void MainWindowLoaded(object sender, RoutedEventArgs e)
		{
			IncomingMessages = _messages;
			Logger.Info("Window is loaded");
			Logger.Info("These messages are logged in ..\\..\\Logs\\NLogSamples.log as well.");
			Logger.Info("I will log when I am resized");
		}

		public ObservableCollection<string> IncomingMessages
		{
			get { return _messages; }
			private set { _messages = value; }
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			// Close listenig.
			// Note: If the listening is not closed, then listening threads are not ended
			//       and the application would not be closed properly.
			myMessageReceiver.DetachDuplexInputChannel();

			myPolicyServer.StopPolicyServer();
		}

		// The method is called when a message from the client is received.
		private void MessageReceived(object sender, StringRequestReceivedEventArgs e)
		{
			// Display received message.
			//InvokeInUIThread(() =>
			//{
			//	ReceivedMessageTextBox.Text = e.RequestMessage;
			//});

			Dispatcher.InvokeAsync(() =>
			{
				ReceivedMessageTextBox.Text = e.RequestMessage;
			});
		}
		 

		// The method is called when a client is connected.
		// The Silverlight client is connected when the client attaches the output duplex channel.
		private void ClientConnected(object sender, ResponseReceiverEventArgs e)
		{
			// Add the connected client to the listbox.
			//InvokeInUIThread(() =>
			Dispatcher.InvokeAsync(() =>
			{
				ConnectedClientsListBox.Items.Add(e.ResponseReceiverId);
			});
		}

		// The method is called when a client is disconnected.
		// The Silverlight client is disconnected if the web page is closed.
		private void ClientDisconnected(object sender, ResponseReceiverEventArgs e)
		{
			// Remove the disconnected client from the listbox.
			//InvokeInUIThread(() =>
			Dispatcher.InvokeAsync(() =>
			{
				ConnectedClientsListBox.Items.Remove(e.ResponseReceiverId);
			});
		}

		private void SendButton_Click(object sender, RoutedEventArgs e)
		{
			// Send the message to all connected clients.
			foreach (string aClientId in ConnectedClientsListBox.Items)
			{
				myMessageReceiver.SendResponseMessage(aClientId, MessageTextBox.Text);
			}
		}

		// Helper method to invoke some functionality in UI thread. 
		//private void InvokeInUIThread(Action uiMethod)
		//{
		//	// If we are not in the UI thread then we must synchronize 
		//	// via the invoke mechanism.
		//	if (InvokeRequired)
		//	{
		//		Invoke(uiMethod);
		//	}
		//	else
		//	{
		//		uiMethod();
		//	}
		//}
	}
}
