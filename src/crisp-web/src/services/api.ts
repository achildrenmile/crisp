import type { Session, ChatMessage, DeliveryCard } from '../types';

const API_BASE = '/api/chat';

export async function createSession(): Promise<Session> {
  const response = await fetch(`${API_BASE}/sessions`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({}),
  });

  if (!response.ok) {
    throw new Error(`Failed to create session: ${response.statusText}`);
  }

  const data = await response.json();
  // Map API response to frontend Session type
  return {
    id: data.sessionId,
    status: data.status.toLowerCase() as Session['status'],
    messages: [],
  };
}

export async function sendMessage(
  sessionId: string,
  content: string
): Promise<ChatMessage> {
  const response = await fetch(`${API_BASE}/sessions/${sessionId}/messages`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ content }),
  });

  if (!response.ok) {
    throw new Error(`Failed to send message: ${response.statusText}`);
  }

  return response.json();
}

export async function getMessages(sessionId: string): Promise<ChatMessage[]> {
  const response = await fetch(`${API_BASE}/sessions/${sessionId}/messages`);

  if (!response.ok) {
    throw new Error(`Failed to get messages: ${response.statusText}`);
  }

  return response.json();
}

export async function getSessionStatus(
  sessionId: string
): Promise<{ status: string }> {
  const response = await fetch(`${API_BASE}/sessions/${sessionId}/status`);

  if (!response.ok) {
    throw new Error(`Failed to get session status: ${response.statusText}`);
  }

  return response.json();
}

export async function approvePlan(
  sessionId: string,
  approved: boolean,
  feedback?: string
): Promise<ChatMessage> {
  const response = await fetch(`${API_BASE}/sessions/${sessionId}/approve`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ approved, feedback }),
  });

  if (!response.ok) {
    throw new Error(`Failed to approve plan: ${response.statusText}`);
  }

  return response.json();
}

export async function getDeliveryResult(
  sessionId: string
): Promise<DeliveryCard | null> {
  const response = await fetch(`${API_BASE}/sessions/${sessionId}/result`);

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Failed to get delivery result: ${response.statusText}`);
  }

  return response.json();
}

export function createEventSource(sessionId: string): EventSource {
  return new EventSource(`${API_BASE}/sessions/${sessionId}/events`);
}
