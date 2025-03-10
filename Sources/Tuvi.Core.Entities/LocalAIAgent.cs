﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Tuvi.Core.Entities
{
    /// <summary>
    /// Represents a local AI agent with a system prompt and an associated email address.
    /// </summary>
    public class LocalAIAgent
    {
        /// <summary>
        /// Gets or sets the ID of the local AI agent.
        /// </summary>
        [SQLite.PrimaryKey]
        [SQLite.AutoIncrement]
        public uint Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the local AI agent.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the specialty of the local AI agent.
        /// </summary>
        public LocalAIAgentSpecialty AgentSpecialty { get; set; }

        /// <summary>
        /// Gets or sets the system prompt for the local AI agent.
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// Gets or sets the email address associated with the local AI agent.
        /// </summary>
        [SQLite.Indexed]
        public int EmailId { get; set; }

        [SQLite.Ignore]
        public EmailAddress Email { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the local AI agent is allowed to send emails.
        /// </summary>
        public bool IsAllowedToSendingEmail { get; set; }

        /// <summary>
        /// Gets or sets the preprocessor agent for the local AI agent.
        /// </summary>
        [SQLite.Ignore]
        public LocalAIAgent PreprocessorAgent { get; set; }

        /// <summary>
        /// Gets or sets the postprocessor agent for the local AI agent.
        /// </summary>
        [SQLite.Ignore]
        public LocalAIAgent PostprocessorAgent { get; set; }

        /// <summary>
        /// Returns the name of the local AI agent.
        /// </summary>
        /// <returns>The name of the local AI agent.</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
