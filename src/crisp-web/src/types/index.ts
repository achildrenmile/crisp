export type SessionStatus =
  | 'intake'
  | 'planning'
  | 'awaiting_approval'
  | 'executing'
  | 'delivering'
  | 'completed'
  | 'failed';

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: string;
  metadata?: MessageMetadata;
}

export interface MessageMetadata {
  plan?: ExecutionPlan;
  deliveryCard?: DeliveryCard;
}

export interface ExecutionPlan {
  steps: PlanStep[];
  policyResults: PolicyResult[];
}

export interface PlanStep {
  number: number;
  description: string;
  status: 'pending' | 'in_progress' | 'completed' | 'failed';
}

export interface PolicyResult {
  rule: string;
  passed: boolean;
  detail?: string;
}

export interface DeliveryCard {
  platform: string;
  repositoryUrl: string;
  branch: string;
  pipelineUrl?: string;
  buildStatus: string;
  vsCodeUrl: string;
}

export interface Session {
  id: string;
  status: SessionStatus;
  messages: ChatMessage[];
  currentPlan?: ExecutionPlan;
  deliveryResult?: DeliveryCard;
}

// SSE Event Types
export type AgentEventType =
  | 'message'
  | 'plan_ready'
  | 'step_started'
  | 'step_completed'
  | 'delivery_ready'
  | 'status_changed'
  | 'error';

export interface AgentEvent {
  type: AgentEventType;
  timestamp: string;
  data: unknown;
}

export interface MessageEvent extends AgentEvent {
  type: 'message';
  data: {
    content: string;
    isPartial: boolean;
  };
}

export interface PlanReadyEvent extends AgentEvent {
  type: 'plan_ready';
  data: ExecutionPlan;
}

export interface StepEvent extends AgentEvent {
  type: 'step_started' | 'step_completed';
  data: {
    stepNumber: number;
    description: string;
    result?: string;
  };
}

export interface DeliveryReadyEvent extends AgentEvent {
  type: 'delivery_ready';
  data: DeliveryCard;
}

export interface StatusChangedEvent extends AgentEvent {
  type: 'status_changed';
  data: {
    status: SessionStatus;
  };
}

export interface ErrorEvent extends AgentEvent {
  type: 'error';
  data: {
    message: string;
    code?: string;
  };
}
