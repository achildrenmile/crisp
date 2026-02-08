import { useState, useEffect, useCallback, useRef } from 'react';
import type {
  Session,
  ChatMessage,
  SessionStatus,
  ExecutionPlan,
  DeliveryCard,
  AgentEvent,
  ActionStatus,
} from '../types';
import * as api from '../services/api';

interface UseSessionReturn {
  session: Session | null;
  messages: ChatMessage[];
  isLoading: boolean;
  error: string | null;
  currentPlan: ExecutionPlan | null;
  deliveryResult: DeliveryCard | null;
  currentAction: ActionStatus | null;
  sendMessage: (content: string) => Promise<void>;
  approvePlan: (approved: boolean, feedback?: string) => Promise<void>;
  createSession: () => Promise<string>;
}

// Map numeric enum values to string status
const statusMap: Record<number, SessionStatus> = {
  0: 'intake',
  1: 'planning',
  2: 'awaiting_approval',
  3: 'executing',
  4: 'delivering',
  5: 'completed',
  6: 'failed',
};

function parseStatus(status: string | number): SessionStatus {
  if (typeof status === 'number') {
    return statusMap[status] || 'intake';
  }
  return status.toLowerCase() as SessionStatus;
}

export function useSession(sessionId?: string): UseSessionReturn {
  const [session, setSession] = useState<Session | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentPlan, setCurrentPlan] = useState<ExecutionPlan | null>(null);
  const [deliveryResult, setDeliveryResult] = useState<DeliveryCard | null>(null);
  const [currentAction, setCurrentAction] = useState<ActionStatus | null>(null);
  const eventSourceRef = useRef<EventSource | null>(null);

  // Handle SSE events
  const handleEvent = useCallback((event: AgentEvent) => {
    switch (event.type) {
      case 'message':
      case 'agent_message': {
        // Handle both legacy 'message' and new 'agent_message' event types
        const eventData = event as unknown as {
          messageId?: string;
          content: string;
          data?: { content: string; isPartial: boolean };
        };
        const content = eventData.content || eventData.data?.content;
        const isPartial = eventData.data?.isPartial ?? false;

        if (content && !isPartial) {
          // Full message - add to messages list
          setMessages((prev) => [
            ...prev,
            {
              id: eventData.messageId || crypto.randomUUID(),
              role: 'assistant',
              content: content,
              timestamp: event.timestamp,
            },
          ]);
          // Clear the action when we get a message
          setCurrentAction(null);
        }
        break;
      }
      case 'plan_ready': {
        const eventData = event as unknown as { plan: ExecutionPlan };
        const plan = eventData.plan;
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
        const eventData = event as unknown as { deliveryCard: DeliveryCard };
        const delivery = eventData.deliveryCard;
        setDeliveryResult(delivery);
        setCurrentAction(null);
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
        setCurrentAction(null);
        break;
      }
      case 'action_status': {
        const actionData = event as unknown as {
          actionKey: string;
          description: string;
        };
        setCurrentAction({
          actionKey: actionData.actionKey,
          description: actionData.description,
        });
        break;
      }
    }
  }, []);

  // Set up SSE connection
  useEffect(() => {
    if (!sessionId) return;

    const eventSource = api.createEventSource(sessionId);
    eventSourceRef.current = eventSource;

    // Handler for parsing and processing SSE events
    const processEvent = (e: MessageEvent) => {
      try {
        const event = JSON.parse(e.data) as AgentEvent;
        handleEvent(event);
      } catch (err) {
        // Ignore parse errors for undefined/empty data
        if (e.data && e.data !== 'undefined') {
          console.error('Failed to parse SSE event:', err);
        }
      }
    };

    // Listen for named events from the server
    const eventTypes = [
      'agent_message',
      'plan_ready',
      'step_started',
      'step_completed',
      'step_failed',
      'build_status',
      'delivery_ready',
      'error',
      'approval_required',
      'action_status',
    ];

    eventTypes.forEach((eventType) => {
      eventSource.addEventListener(eventType, processEvent);
    });

    // Also handle unnamed events (fallback)
    eventSource.onmessage = processEvent;

    eventSource.onerror = () => {
      console.error('SSE connection error');
      // EventSource will automatically reconnect
    };

    return () => {
      eventTypes.forEach((eventType) => {
        eventSource.removeEventListener(eventType, processEvent);
      });
      eventSource.close();
      eventSourceRef.current = null;
    };
  }, [sessionId, handleEvent]);

  // Load initial session data (messages, status, and delivery result)
  useEffect(() => {
    if (!sessionId) return;

    // Reset state when session changes
    setDeliveryResult(null);
    setCurrentPlan(null);
    setCurrentAction(null);
    setError(null);

    const loadSessionData = async () => {
      try {
        // Load messages
        const msgs = await api.getMessages(sessionId);
        setMessages(msgs);

        // Load session status
        const statusData = await api.getSessionStatus(sessionId);
        const status = parseStatus(statusData.status);
        setSession({
          id: sessionId,
          status,
          messages: msgs,
        });

        // If session is completed, load delivery result
        if (status === 'completed') {
          const result = await api.getDeliveryResult(sessionId);
          if (result) {
            setDeliveryResult(result);
          }
        }
      } catch (err) {
        console.error('Failed to load session data:', err);
      }
    };

    loadSessionData();
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
        const response = await api.sendMessage(sessionId, content);
        // Add assistant response from API
        if (response && response.content) {
          const assistantMessage: ChatMessage = {
            id: response.messageId || crypto.randomUUID(),
            role: 'assistant',
            content: response.content,
            timestamp: response.timestamp || new Date().toISOString(),
          };
          setMessages((prev) => [...prev, assistantMessage]);
        }
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
    currentAction,
    sendMessage,
    approvePlan,
    createSession,
  };
}
