namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

public class SessionRunnerFactory : ISessionRunnerFactory
{
    private readonly IAIProvider aiProvider;
    private readonly ILoggerFactory loggerFactory;
    private readonly IStorageProvider storageProvider;

    public SessionRunnerFactory(IAIProvider aiProvider, ILoggerFactory loggerFactory, IStorageProvider storageProvider)
    {
        this.aiProvider = aiProvider;
        this.loggerFactory = loggerFactory;
        this.storageProvider = storageProvider;
    }

    public SessionRunner Create(List<Agent> agents, SessionPremise premise)
    {
        var logger = loggerFactory.CreateLogger<SessionRunner>();
        return new SessionRunner(agents, premise, aiProvider, logger);
    }
    
    public SessionRunner CreateFromExistingSession(string sessionId, List<Agent> agents, SessionPremise premise)
    {
        var logger = loggerFactory.CreateLogger<SessionRunner>();
        var existingSession = storageProvider.LoadSession(sessionId);
        return new SessionRunner(agents, premise, aiProvider, logger, existingSession);
    }
}
