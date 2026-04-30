var chatForm = document.getElementById("chatForm");
var messageInput = document.getElementById("messageInput");
var chatBox = document.getElementById("chatBox");
var API_URL = "/api/chat";

function addMessage(role, content) {
  var messageDiv = document.createElement("div");
  messageDiv.className = "message " + role;
  messageDiv.innerText = content;
  chatBox.appendChild(messageDiv);
  chatBox.scrollTop = chatBox.scrollHeight;
}

chatForm.addEventListener("submit", function (event) {
  event.preventDefault();

  var message = messageInput.value.trim();
  if (!message) {
    return;
  }

  addMessage("user", message);
  messageInput.value = "";

  fetch(API_URL, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ message: message })
  })
    .then(function (response) {
      if (!response.ok) {
        return response.text().then(function (text) {
          throw new Error(text || ("Status " + response.status));
        });
      }
      return response.json();
    })
    .then(function (data) {
      if (data.reply) {
        addMessage("assistant", data.reply);
      } else if (data.detail) {
        addMessage("assistant", "Backend error: " + data.detail);
      } else {
        addMessage("assistant", "No response from assistant.");
      }
    })
    .catch(function (error) {
      console.error(error);
      addMessage("assistant", "Backend call failed: " + error.message);
    });
});
