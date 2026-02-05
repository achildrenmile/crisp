import { useState, useEffect, useCallback, useRef } from 'react';
import type {
  Session,
  ChatMessage,
  SessionStatus,
  ExecutionPlan,
  DeliveryCard,
  AgentEvent,
} from '../types';
import * as api from '../services/api';

interface UseSessionReturn {
  session: Session | null;
  messages: ChatMessage[];
  isLoading: boolean;
  error: string | null;
  currentPlan: ExecutionPlan | null;
  deliveryResult: DeliveryCard | null;
  sendMessage: (content: string) => Promise<void>;
  approvePlan: (approved: boolean, feedback?: string) => Promise<void>;
  createSession: () => Promise<string>;
}

export function useSession(sessionId?: string): UseSessionReturn {
  const [session, setSession] = useState<Session | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentPlan, setCurrentPlan] = useState<ExecutionPlan | null>(null);
  const [deliveryResult, setDeliveryResult] = useState<DeliveryCard | null>(null);
  const eventSourceRef = useRef<EventSource | null>(null);

  // Handle SSE events
  const handleEvent = useCallback((event: AgentEvent) => {
    switch (event.type) {
      case 'message': {
        const data = event.data as { content: string; isPartial: boolean };
        if (!data.isPartial) {
          // Full message - add to messages list
          setMessages((prev) => [
            ...prev,
            {
              id: crypto.randomUUID(),
              role: 'assistant',
              content: data.content,
              timestamp: event.timestamp,
            },
          ]);
        }
        break;
      }
      case 'plan_ready': {
        const plan = event.data as ExecutionPlan;
        setCurrentPlan(plan);
        setSession((prev) =>
          prev ? { ...prev, status: 'awaiting_approval' as SessionStatus } : prev
        );
        break;
      }
      case 'step_started':
      case 'step_completed': {
        const stepData = event.data as {
          stepNumber: number;
          description: string;
          result?: string;
        };
        setCurrentPlan((prev) => {
          if (!prev) return prev;
          return {
            ...prev,
            steps: prev.steps.map((step) =>
              step.number === stepData.stepNumber
                ? {
                    ...step,
                    status:
                      event.type === 'step_started' ? 'in_progress' : 'completed',
                  }
                : step
            ),
          };
        });
        break;
      }
      case 'delivery_ready': {
        const delivery = event.data as DeliveryCard;
        setDeliveryResult(delivery);
        setSession((prev) =>
          prev ? { ...prev, status: 'completed' as SessionStatus } : prev
        );
        break;
      }
      case 'status_changed': {
        const statusData = event.data as { status: SessionStatus };
        setSession((prev) =>
          prev ? { ...prev, status: statusData.status } : prev
        );
        break;
      }
      case 'error': {
        const errorData = event.data as { message: string };
        setError(errorData.message);
        setIsLoading(false);
        break;
      }
    }
  }, []);

  // Set up SSE connection
  useEffect(() => {
    if (!sessionId) return;

    const eventSource = api.createEventSource(sessionId);
    eventSourceRef.current = eventSource;

    eventSource.onmessage = (e) => {
      try {
        const event = JSON.parse(e.data) as AgentEvent;
        handleEvent(event);
      } catch (err) {
        console.error('Failed to parse SSE event:', err);
      }
    };

    eventSource.onerror = () => {
      console.error('SSE connection error');
      // EventSource will automatically reconnect
    };

    return () => {
      eventSource.close();
      eventSourceRef.current = null;
    };
  }, [sessionId, handleEvent]);

  // Load initial messages
  useEffect(() => {
    if (!sessionId) return;

    const loadMessages = async () => {
      try {
        const msgs = await api.getMessages(sessionId);
        setMessages(msgs);
      } catch (err) {
        console.error('Failed to load messages:', err);
      }
    };

    loadMessages();
  }, [sessionId]);

  const createSession = useCallback(async (): Promise<string> => {
    setIsLoading(true);
    setError(null);

    try {
      const newSession = await api.createSession();
      setSession(newSession);
      setMessages([]);
      setCurrentPlan(null);
      setDeliveryResult(null);
      return newSession.id;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create session';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const sendMessage = useCallback(
    async (content: string) => {
      if (!sessionId) {
        setError('No active session');
        return;
      }

      setIsLoading(true);
      setError(null);

      // Optimistically add user message
      const userMessage: ChatMessage = {
        id: crypto.randomUUID(),
        role: 'user',
        content,
        timestamp: new Date().toISOString(),
      };
      setMessages((prev) => [...prev, userMessage]);

      try {
        await api.sendMessage(sessionId, content);
        // Response will come via SSE
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to send message';
        setError(message);
        // Remove optimistic message on error
        setMessages((prev) => prev.filter((m) => m.id !== userMessage.id));
      } finally {
        setIsLoading(false);
      }
    },
    [sessionId]
  );

  const approvePlan = useCallback(
    async (approved: boolean, feedback?: string) => {
      if (!sessionId) {
        setError('No active session');
        return;
      }

      setIsLoading(true);
      setError(null);

      try {
        await api.approvePlan(sessionId, approved, feedback);
        // Status change will come via SSE
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to approve plan';
        setError(message);
      } finally {
        setIsLoading(false);
      }
    },
    [sessionId]
  );

  return {
    session,
    messages,
    isLoading,
    error,
    currentPlan,
    deliveryResult,
    sendMessage,
    approvePlan,
    createSession,
  };
}
