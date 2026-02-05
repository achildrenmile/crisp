import type { ExecutionPlan } from '../types';

interface PlanViewProps {
  plan: ExecutionPlan;
  isAwaitingApproval: boolean;
  onApprove: () => void;
  onReject: () => void;
}

export function PlanView({
  plan,
  isAwaitingApproval,
  onApprove,
  onReject,
}: PlanViewProps) {
  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
        return '✅';
      case 'in_progress':
        return '🔄';
      case 'failed':
        return '❌';
      default:
        return '⏳';
    }
  };

  return (
    <div className="plan-view">
      <div className="plan-header">
        <h3>Execution Plan</h3>
      </div>

      <div className="plan-steps">
        {plan.steps.map((step) => (
          <div
            key={step.number}
            className={`plan-step step-${step.status.toLowerCase()}`}
          >
            <span className="step-number">{step.number}</span>
            <span className="step-description">{step.description}</span>
            <span className="step-status">{getStatusIcon(step.status)}</span>
          </div>
        ))}
      </div>

      {plan.policyResults.length > 0 && (
        <div className="policy-results">
          <h4>Policy Checks</h4>
          {plan.policyResults.map((policy, index) => (
            <div
              key={index}
              className={`policy-result ${policy.passed ? 'passed' : 'failed'}`}
            >
              <span className="policy-icon">{policy.passed ? '✅' : '❌'}</span>
              <span className="policy-rule">{policy.rule}</span>
              {policy.detail && (
                <span className="policy-detail">{policy.detail}</span>
              )}
            </div>
          ))}
        </div>
      )}

      {isAwaitingApproval && (
        <div className="plan-actions">
          <button className="btn-primary" onClick={onApprove}>
            ✓ Approve & Execute
          </button>
          <button className="btn-secondary" onClick={onReject}>
            ✗ Request Changes
          </button>
        </div>
      )}
    </div>
  );
}
