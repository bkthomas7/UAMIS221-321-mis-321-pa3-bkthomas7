CREATE DATABASE IF NOT EXISTS mis321_pa3;
USE mis321_pa3;

CREATE TABLE IF NOT EXISTS conversation_history (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_message TEXT NOT NULL,
  assistant_reply TEXT NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS knowledge_base (
  id INT AUTO_INCREMENT PRIMARY KEY,
  topic VARCHAR(100) NOT NULL,
  content TEXT NOT NULL
);

INSERT INTO knowledge_base (topic, content) VALUES
('llm', 'Large Language Models generate text responses from prompts.'),
('rag', 'RAG means retrieval augmented generation: retrieve useful context before generating an answer.'),
('function calling', 'Function calling allows the model to request specific tools such as database checks or calculations.');
