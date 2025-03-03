namespace AIStorm.Core.Storage.Markdown;

using AIStorm.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AIStorm.Core.Storage;

public class MarkdownStorageProvider : IStorageProvider
{
    private readonly string basePath;
    private readonly string agentTemplatesPath;
    private readonly string sessionsPath;
    private readonly MarkdownSerializer serializer;
    private readonly ILogger<MarkdownStorageProvider> logger;

    public MarkdownStorageProvider(
        IOptions<MarkdownStorageOptions> options, 
        MarkdownSerializer serializer,
        ILogger<MarkdownStorageProvider> logger)
    {
        var storageOptions = options.Value;
        
        if (string.IsNullOrEmpty(storageOptions.BasePath))
            throw new ArgumentException("Base path is required", nameof(options));
            
        this.basePath = Path.GetFullPath(storageOptions.BasePath);
        Directory.CreateDirectory(this.basePath);
        
        this.agentTemplatesPath = Path.Combine(this.basePath, "AgentTemplates");
        this.sessionsPath = Path.Combine(this.basePath, "Sessions");
        this.serializer = serializer;
        this.logger = logger;
        
        Directory.CreateDirectory(this.agentTemplatesPath);
        Directory.CreateDirectory(this.sessionsPath);
        
        logger.LogInformation("MarkdownStorageProvider initialized with base path: {BasePath}", this.basePath);
    }
    
    private string GetAgentPath(string id) => Path.Combine(agentTemplatesPath, id + ".md");

    private string GetSessionPath(string id) => Path.Combine(sessionsPath, id + ".session.md");

    private string ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found. Path: {path}", path);
        }
        return File.ReadAllText(path);
    }

    private void WriteFile(string path, string content)
    {
        string? directoryName = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
        File.WriteAllText(path, content);
    }


    public Agent LoadAgent(string id)
    {
        var fullPath = GetAgentPath(id);
        var content = ReadFile(fullPath);
        
        var segments = MarkdownSegment.ParseSegments(content, serializer, throwOnNone: true);
        return segments.Single().ToAgent();
    }

    public void SaveAgent(string id, Agent agent)
    {
        var fullPath = GetAgentPath(id);
        var segment = MarkdownSegment.FromAgent(agent);
        var content = segment.ToMarkdown(serializer);
        WriteFile(fullPath, content);
    }

    public Session LoadSession(string id)
    {
        logger.LogInformation("LoadSession called with ID: '{SessionId}'", id);
        
        var fullPath = GetSessionPath(id);
        logger.LogInformation("Attempting to load session from path: '{FullPath}'", fullPath);
        
        var content = ReadFile(fullPath);
        var segments = MarkdownSegment.ParseSegments(content, serializer, throwOnNone: true);
        
        var created = segments
            .Single(s => s.GetSegmentType() == "session")
            .GetRequiredTimestampUtc("created");
        
        var premise = segments
            .Single(s => s.GetSegmentType() == "premise")
            .ToPremise(id);
        
        var agents = segments
            .Where(s => s.GetSegmentType() == "agent")
            .Select(s => s.ToAgent())
            .ToList();
        
        var messages = segments
            .Where(s => s.GetSegmentType() == "message")
            .Select(s => s.ToStormMessage())
            .ToList();
        
        return new Session(id, created, premise, agents, messages);
    }

    public void SaveSession(string id, Session session)
    {
        var fullPath = GetSessionPath(id);
        var segments = new List<MarkdownSegment>
        {
            MarkdownSegment.FromSessionMetadata(id, session)
        };
        
        segments.Add(MarkdownSegment.FromPremise(session.Premise));
        
        segments.AddRange(session.Agents.Select(MarkdownSegment.FromAgent));
        segments.AddRange(session.Messages.Select(MarkdownSegment.FromMessage));
        
        var content = serializer.SerializeDocument(segments);
        WriteFile(fullPath, content);
    }
}
