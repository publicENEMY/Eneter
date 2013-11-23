using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;

namespace SilverlightClient
{
	public partial class MainPage : UserControl
	{
		public MainPage()
		{
			InitializeComponent();

			// Create duplex message sender.
			// It can send messages and also receive messages.
			IDuplexStringMessagesFactory aStringMessagesFactory = new DuplexStringMessagesFactory();
			myMessageSender = aStringMessagesFactory.CreateDuplexStringMessageSender();
			myMessageSender.ResponseReceived += MessageReceived;

			// Create TCP based messaging.
			IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
			IDuplexOutputChannel aDuplexOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:4502/");

			// Attach the duplex output channel to the message sender
			// and be able to send messages and receive messages.
			myMessageSender.AttachDuplexOutputChannel(aDuplexOutputChannel);
		}

		// The method is called when a message from the desktop application is received.
		private void MessageReceived(object sender, StringResponseReceivedEventArgs e)
		{
			textBox2.Text = e.ResponseMessage;
		}

		// The method is called when the button to send message is clicked.
		private void SendMessage_Click(object sender, RoutedEventArgs e)
		{
			myMessageSender.SendMessage(textBox1.Text);
		}
		
		private IDuplexStringMessageSender myMessageSender;

	}
}
