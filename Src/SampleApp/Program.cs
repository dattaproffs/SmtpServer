﻿using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmtpServer;
using SmtpServer.Tracing;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using ContentEncoding = SmtpServer.Protocol.ContentEncoding;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var certificate = CreateCertificate();

            ServicePointManager.ServerCertificateValidationCallback = IgnoreCertificateValidationFailureForTestingOnly;

            var options = new OptionsBuilder()
                .ServerName("SmtpServer SampleApp")
                .Port(9025)
                .Certificate(certificate)
                .SupportedSslProtocols(SslProtocols.Default)
                .MessageStore(new ConsoleMessageStore())
                .MailboxFilter(new ConsoleMailboxFilter())
                .DefaultContentEncoding(ContentEncoding.EightBit)
                .Build();

            var s = RunServerAsync(options, cancellationTokenSource.Token);
            var c = RunClientAsync("A", 1, cancellationTokenSource.Token);

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();

            cancellationTokenSource.Cancel();

            s.WaitWithoutException();
            c.WaitWithoutException();

            //return;

            //if (args == null || args.Length == 0)
            //{
            //    var serverTask = RunServerAsync(options, cancellationTokenSource.Token);
            //    var clientTask1 = RunClientAsync("A", cancellationToken: cancellationTokenSource.Token);
            //    var clientTask2 = RunClientAsync("B", cancellationToken: cancellationTokenSource.Token);
            //    var clientTask3 = RunClientAsync("C", cancellationToken: cancellationTokenSource.Token);

            //    Console.WriteLine("Press any key to continue");
            //    Console.ReadKey();

            //    cancellationTokenSource.Cancel();

            //    serverTask.WaitWithoutException();
            //    clientTask1.WaitWithoutException();
            //    clientTask2.WaitWithoutException();
            //    clientTask3.WaitWithoutException();

            //    return;
            //}

            //if (args[0] == "server")
            //{
            //    var serverTask = RunServerAsync(options, cancellationToken: cancellationTokenSource.Token);

            //    Console.WriteLine("Press any key to continue");
            //    Console.ReadKey();

            //    cancellationTokenSource.Cancel();

            //    serverTask.WaitWithoutException();

            //    return;
            //}

            //if (args[0] == "client")
            //{
            //    var clientTask = RunClientAsync(args[1], cancellationToken: cancellationTokenSource.Token);

            //    Console.WriteLine("Press any key to continue");
            //    Console.ReadKey();

            //    cancellationTokenSource.Cancel();

            //    clientTask.WaitWithoutException();
            //}
        }

        static async Task RunServerAsync(ISmtpServerOptions options, CancellationToken cancellationToken)
        {
            var smtpServer = new SmtpServer.SmtpServer(options);

            smtpServer.SessionCreated += OnSmtpServerSessionCreated;
            smtpServer.SessionCompleted += OnSmtpServerSessionCompleted;

            await smtpServer.StartAsync(cancellationToken);

            smtpServer.SessionCreated -= OnSmtpServerSessionCreated;
            smtpServer.SessionCompleted -= OnSmtpServerSessionCompleted;
        }

        static async Task RunClientAsync(string name, int maximum = Int32.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            var counter = 0;
            while (counter++ < maximum && cancellationToken.IsCancellationRequested == false)
            {
                using (var smtpClient = new SmtpClient())
                {
                    await smtpClient.ConnectAsync("localhost", 9025, false, cancellationToken);

                    Console.WriteLine();
                    Console.WriteLine("Client has Connected.");
                    Console.WriteLine(smtpClient.Capabilities);
                    Console.WriteLine(smtpClient.IsSecure);
                    Console.WriteLine();

                    try
                    {
                        var message = new MimeKit.MimeMessage();
                        message.From.Add(new MimeKit.MailboxAddress($"{name}{counter}@test.com"));
                        message.To.Add(new MimeKit.MailboxAddress("sample@test.com"));
                        message.Subject = $"Subject test çãõáéíóú";

                        message.Body = new TextPart(TextFormat.Plain)
                        {
                            Text = "Test Message Body special char çãõáéíóú",
                        };

                        await smtpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }

                    await smtpClient.DisconnectAsync(true, cancellationToken);
                }

                counter++;
            }
        }

        static void OnSmtpServerSessionCreated(object sender, SessionEventArgs e)
        {
            Console.WriteLine("SessionCreated: {0}", e.Context.RemoteEndPoint);

            e.Context.CommandExecuting += OnCommandExecuting;
        }

        static void OnCommandExecuting(object sender, SmtpCommandExecutingEventArgs e)
        {
            new TracingSmtpCommandVisitor(Console.Out).Visit(e.Command);
        }

        static void OnSmtpServerSessionCompleted(object sender, SessionEventArgs e)
        {
            e.Context.CommandExecuting -= OnCommandExecuting;

            Console.WriteLine("SessionCompleted: {0}", e.Context.RemoteEndPoint);
        }

        static bool IgnoreCertificateValidationFailureForTestingOnly(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        
        static X509Certificate2 CreateCertificate()
        {
            // to create an X509Certificate for testing you need to run MAKECERT.EXE and then PVK2PFX.EXE
            // http://www.digitallycreated.net/Blog/38/using-makecert-to-create-certificates-for-development

            var certificate = File.ReadAllBytes(@"C:\Dropbox\Documents\Cain\Programming\SmtpServer\SmtpServer.pfx");
            var password = File.ReadAllText(@"C:\Dropbox\Documents\Cain\Programming\SmtpServer\SmtpServerPassword.txt");

            return new X509Certificate2(certificate, password);
        }
    }
}
