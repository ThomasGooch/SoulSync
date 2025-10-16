using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Communication;

public class MessageProcessorAgent : BaseAgent
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<MessageProcessorAgent> _logger;

    public MessageProcessorAgent(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        ILogger<MessageProcessorAgent> logger)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Validate parameters
        if (!request.Parameters.TryGetValue("senderId", out var senderIdObj) ||
            !Guid.TryParse(senderIdObj?.ToString(), out var senderId))
        {
            return AgentResult.CreateError("Missing or invalid senderId parameter");
        }

        if (!request.Parameters.TryGetValue("receiverId", out var receiverIdObj) ||
            !Guid.TryParse(receiverIdObj?.ToString(), out var receiverId))
        {
            return AgentResult.CreateError("Missing or invalid receiverId parameter");
        }

        if (!request.Parameters.TryGetValue("conversationId", out var conversationIdObj) ||
            !Guid.TryParse(conversationIdObj?.ToString(), out var conversationId))
        {
            return AgentResult.CreateError("Missing or invalid conversationId parameter");
        }

        if (!request.Parameters.TryGetValue("content", out var contentObj) ||
            string.IsNullOrWhiteSpace(contentObj?.ToString()))
        {
            return AgentResult.CreateError("Content cannot be empty");
        }

        var content = contentObj.ToString()!;

        // 2. Verify users exist
        var sender = await _userRepository.GetByIdAsync(senderId, cancellationToken);
        if (sender == null)
        {
            return AgentResult.CreateError($"Sender user not found: {senderId}");
        }

        var receiver = await _userRepository.GetByIdAsync(receiverId, cancellationToken);
        if (receiver == null)
        {
            return AgentResult.CreateError($"Receiver user not found: {receiverId}");
        }

        // 3. Verify conversation exists
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            return AgentResult.CreateError($"Conversation not found: {conversationId}");
        }

        // 4. Create and save the message
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            ConversationId = conversationId,
            Content = content
        };

        var createdMessage = await _messageRepository.CreateAsync(message, cancellationToken);

        // 5. Update conversation
        conversation.AddMessage();
        await _conversationRepository.UpdateAsync(conversation, cancellationToken);

        _logger.LogInformation("Message {MessageId} processed successfully from {SenderId} to {ReceiverId}",
            createdMessage.Id, senderId, receiverId);

        // 6. Return result
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["messageId"] = createdMessage.Id,
            ["status"] = createdMessage.Status,
            ["createdAt"] = createdMessage.CreatedAt
        });
    }
}
