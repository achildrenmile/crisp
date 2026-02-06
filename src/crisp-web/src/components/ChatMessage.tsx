import { useState, useRef } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Copy, Check, Code } from 'lucide-react';
import type { ChatMessage as ChatMessageType } from '../types';

interface ChatMessageProps {
  message: ChatMessageType;
}

interface CodeBlockProps {
  children: string;
}

function CodeBlock({ children }: CodeBlockProps) {
  const [copied, setCopied] = useState(false);
  const codeRef = useRef<HTMLElement>(null);

  const handleCopy = async () => {
    const textToCopy = codeRef.current?.textContent || children;
    try {
      await navigator.clipboard.writeText(textToCopy);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  return (
    <div className="code-block-wrapper">
      <pre>
        <code ref={codeRef}>{children}</code>
      </pre>
      <button
        className={`copy-btn ${copied ? 'copied' : ''}`}
        onClick={handleCopy}
        title={copied ? 'Copied!' : 'Copy to clipboard'}
      >
        {copied ? <Check size={14} /> : <Copy size={14} />}
      </button>
    </div>
  );
}

export function ChatMessage({ message }: ChatMessageProps) {
  const isUser = message.role === 'user';
  const timestamp = new Date(message.timestamp).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  });

  return (
    <div className={`message message-${message.role}`}>
      <div className="message-content">
        {isUser ? (
          <p>{message.content}</p>
        ) : (
          <ReactMarkdown
            remarkPlugins={[remarkGfm]}
            urlTransform={(url) => url}
            components={{
              a: ({ href, children }) => {
                // Check if this is a VS Code web link (vscode.dev or github.dev)
                const isVsCodeWebLink = href && (
                  href.startsWith('https://vscode.dev/') ||
                  href.startsWith('https://github.dev/')
                );

                // Check if this is a VS Code clone protocol link
                const isVsCodeCloneLink = href && href.startsWith('vscode://vscode.git/clone');

                // Check if this is a protocol link (vscode://, etc.)
                const isProtocolLink = href && !href.startsWith('http://') && !href.startsWith('https://') && href.includes('://');

                // Render VS Code web links as styled buttons (browser)
                if (isVsCodeWebLink) {
                  return (
                    <a
                      href={href}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="vscode-btn"
                      title="Opens instantly in browser"
                    >
                      <Code size={14} />
                      {children}
                    </a>
                  );
                }

                // Handle VS Code clone protocol links with distinct styling
                if (isVsCodeCloneLink) {
                  return (
                    <a
                      href={href}
                      className="vscode-clone-btn"
                      title="Clones to your local VS Code"
                    >
                      <Code size={14} />
                      {children}
                    </a>
                  );
                }

                if (isProtocolLink) {
                  return (
                    <a
                      href={href}
                      onClick={(e) => {
                        e.preventDefault();
                        const link = document.createElement('a');
                        link.href = href;
                        link.click();
                      }}
                      style={{ cursor: 'pointer' }}
                    >
                      {children}
                    </a>
                  );
                }

                return (
                  <a href={href} target="_blank" rel="noopener noreferrer">
                    {children}
                  </a>
                );
              },
              code: ({ children, className, node, ...props }) => {
                // Check if this code block is inside a pre tag (block code)
                const isBlock = node?.position &&
                  String(children).includes('\n') || className?.startsWith('language-');

                if (isBlock || className) {
                  return <CodeBlock>{String(children).replace(/\n$/, '')}</CodeBlock>;
                }
                // Inline code
                return <code className="inline-code" {...props}>{children}</code>;
              },
              pre: ({ children }) => {
                // Just return children since CodeBlock handles the pre wrapper
                return <>{children}</>;
              },
            }}
          >
            {message.content}
          </ReactMarkdown>
        )}
      </div>
      <div className="message-timestamp">{timestamp}</div>
    </div>
  );
}
