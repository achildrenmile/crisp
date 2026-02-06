import { Sun, Moon, Monitor } from 'lucide-react';
import { useTheme, type Theme } from '../contexts/ThemeContext';

const themes: { value: Theme; icon: typeof Sun; label: string }[] = [
  { value: 'auto', icon: Monitor, label: 'Auto' },
  { value: 'light', icon: Sun, label: 'Light' },
  { value: 'dark', icon: Moon, label: 'Dark' },
];

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  const cycleTheme = () => {
    const currentIndex = themes.findIndex((t) => t.value === theme);
    const nextIndex = (currentIndex + 1) % themes.length;
    setTheme(themes[nextIndex].value);
  };

  const currentTheme = themes.find((t) => t.value === theme) || themes[0];
  const Icon = currentTheme.icon;

  return (
    <button
      onClick={cycleTheme}
      className="theme-toggle"
      title={`Theme: ${currentTheme.label} (click to change)`}
      aria-label={`Current theme: ${currentTheme.label}. Click to change theme.`}
    >
      <Icon size={18} />
    </button>
  );
}
