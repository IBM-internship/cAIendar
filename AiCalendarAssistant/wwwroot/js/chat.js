// Chat Window Management
class ChatWindow {
    constructor() {
        this.chats = [];
        this.selectedChatId = null;
        this.initializeEventListeners();
        this.loadChats();
    }

    initializeEventListeners() {
        // Chat window toggle
        document.getElementById('chatToggle').addEventListener('click', () => {
            this.openChat();
        });

        document.getElementById('chatClose').addEventListener('click', () => {
            this.closeChat();
        });

        document.getElementById('chatOverlay').addEventListener('click', (e) => {
            if (e.target.id === 'chatOverlay') {
                this.closeChat();
            }
        });

        // Chat functionality
        document.getElementById('newChatBtn').addEventListener('click', () => {
            this.createNewChat();
        });

        document.getElementById('deleteChatBtn').addEventListener('click', () => {
            this.deleteSelectedChat();
        });

        document.getElementById('sendBtn').addEventListener('click', () => {
            this.sendMessage();
        });

        document.getElementById('messageInput').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.sendMessage();
            }
        });
    }

    openChat() {
        const overlay = document.getElementById('chatOverlay');
        overlay.classList.add('show');
        document.body.style.overflow = 'hidden';
    }

    closeChat() {
        const overlay = document.getElementById('chatOverlay');
        overlay.classList.remove('show');
        document.body.style.overflow = 'auto';
    }

    loadChats() {
        // Mock data - replace with actual API call
        this.chats = [
            { id: 1, title: 'Chat 1', messages: [] },
            {
                id: 2, title: 'Chat 2', messages: [
                    { role: 'User', text: 'Hello!', sentOn: new Date() },
                    { role: 'Assistant', text: 'Hi there! How can I help you?', sentOn: new Date() }
                ]
            }
        ];
        this.renderChatList();
    }

    renderChatList() {
        const chatList = document.getElementById('chatList');
        chatList.innerHTML = '';

        this.chats.forEach(chat => {
            const chatItem = document.createElement('a');
            chatItem.href = '#';
            chatItem.className = `chat-item ${chat.id === this.selectedChatId ? 'active' : ''}`;
            chatItem.innerHTML = `
                        <i class="fas fa-comment-dots me-2"></i>
                        ${chat.title || `Chat ${chat.id}`}
                    `;
            chatItem.addEventListener('click', (e) => {
                e.preventDefault();
                this.selectChat(chat.id);
            });
            chatList.appendChild(chatItem);
        });
    }

    selectChat(chatId) {
        this.selectedChatId = chatId;
        this.renderChatList();
        this.renderMessages();
        this.updateUI();
    }

    renderMessages() {
        const messagesContainer = document.getElementById('messagesContainer');
        const noMessages = document.getElementById('noMessages');
        const selectedChat = this.chats.find(c => c.id === this.selectedChatId);

        if (!selectedChat || selectedChat.messages.length === 0) {
            messagesContainer.innerHTML = '';
            messagesContainer.appendChild(noMessages);
            return;
        }

        messagesContainer.innerHTML = '';
        selectedChat.messages.forEach(message => {
            const messageDiv = document.createElement('div');
            messageDiv.className = 'message';
            messageDiv.innerHTML = `
                        <div class="message-role">${message.role}:</div>
                        <div class="message-text">${message.text}</div>
                        <div class="message-time">${message.sentOn.toLocaleTimeString()}</div>
                    `;
            messagesContainer.appendChild(messageDiv);
        });

        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    updateUI() {
        const deleteChatBtn = document.getElementById('deleteChatBtn');
        const messageInputContainer = document.getElementById('messageInputContainer');

        if (this.selectedChatId) {
            deleteChatBtn.style.display = 'block';
            messageInputContainer.style.display = 'flex';
        } else {
            deleteChatBtn.style.display = 'none';
            messageInputContainer.style.display = 'none';
        }
    }

    createNewChat() {
        const newChatId = Math.max(...this.chats.map(c => c.id), 0) + 1;
        const newChat = {
            id: newChatId,
            title: `Chat ${newChatId}`,
            messages: []
        };

        this.chats.push(newChat);
        this.selectChat(newChatId);

        // In real implementation, make API call to create chat
        // fetch('/api/chat/create', { method: 'POST' })
    }

    deleteSelectedChat() {
        if (!this.selectedChatId) return;

        if (confirm('Are you sure you want to delete this chat?')) {
            this.chats = this.chats.filter(c => c.id !== this.selectedChatId);
            this.selectedChatId = null;
            this.renderChatList();
            this.renderMessages();
            this.updateUI();

            // In real implementation, make API call to delete chat
            // fetch(`/api/chat/delete/${this.selectedChatId}`, { method: 'DELETE' })
        }
    }

    async sendMessage() {
        const messageInput = document.getElementById('messageInput');
        const text = messageInput.value.trim();

        if (!text || !this.selectedChatId) return;

        const selectedChat = this.chats.find(c => c.id === this.selectedChatId);
        if (!selectedChat) return;

        // Add user message
        selectedChat.messages.push({
            role: 'User',
            text: text,
            sentOn: new Date()
        });

        messageInput.value = '';
        this.renderMessages();

        // Disable send button while processing
        const sendBtn = document.getElementById('sendBtn');
        sendBtn.disabled = true;
        sendBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Sending...';

        try {
            // In real implementation, make API call
            // const response = await fetch('/api/chat', {
            //     method: 'POST',
            //     headers: { 'Content-Type': 'application/json' },
            //     body: JSON.stringify({ text: text, chatId: this.selectedChatId })
            // });
            // const responseText = await response.text();

            // Mock response
            setTimeout(() => {
                const mockResponse = `This is a mock response to: "${text}"`;
                selectedChat.messages.push({
                    role: 'Assistant',
                    text: mockResponse,
                    sentOn: new Date()
                });
                this.renderMessages();

                // Re-enable send button
                sendBtn.disabled = false;
                sendBtn.innerHTML = '<i class="fas fa-paper-plane"></i> Send';
            }, 1000);

        } catch (error) {
            console.error('Error sending message:', error);
            selectedChat.messages.push({
                role: 'System',
                text: 'Error sending message. Please try again.',
                sentOn: new Date()
            });
            this.renderMessages();

            // Re-enable send button
            sendBtn.disabled = false;
            sendBtn.innerHTML = '<i class="fas fa-paper-plane"></i> Send';
        }
    }
}

// Initialize chat window when page loads
document.addEventListener('DOMContentLoaded', () => {
    new ChatWindow();
});