﻿using System.Threading;
using System.Threading.Tasks;
using SmtpServer.Mail;

namespace SmtpServer.Storage
{
    public interface IMessageStore
    {
        /// <summary>
        /// Creates an instance of the message store specifically for this session.
        /// </summary>
        /// <param name="context">The session level context.</param>
        /// <returns>The message store instance specifically for this session.</returns>
        IMessageStore CreateSessionInstance(ISessionContext context);

        /// <summary>
        /// Save the given message to the underlying storage system.
        /// </summary>
        /// <param name="message">The SMTP message to store.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unique identifier that represents this message in the underlying message store.</returns>
        Task<string> SaveAsync(IMimeMessage message, CancellationToken cancellationToken);
    }
}