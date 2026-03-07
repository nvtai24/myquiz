import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

interface Achievement {
  icon: string;
  label: string;
  date: string;
  unlocked: boolean;
}

interface Activity {
  icon: string;
  color: string;
  text: string;
  detail: string;
}

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './user-profile.html',
  styleUrl: './user-profile.css',
})
export class UserProfile {
  user = {
    name: 'Alex Johnson',
    role: 'STUDENT',
    email: 'alex.j@quizmate.com',
    bio: 'Passionate learner and quiz enthusiast. Currently mastering Advanced Web Development and Data Science. Aiming for a 100-day streak!',
  };

  stats = [
    { label: 'Sets', value: '12', icon: 'style' },
    { label: 'Quizzes', value: '47', icon: 'quiz' },
    { label: 'Study Time', value: '24h', icon: 'schedule' },
    { label: 'Streak', value: '7 days', icon: 'local_fire_department' },
  ];

  achievements: Achievement[] = [
    { icon: 'emoji_events', label: 'First Quiz', date: '11/2023', unlocked: true },
    { icon: 'star', label: 'Perfect Score', date: '12/2023', unlocked: true },
    { icon: 'bolt', label: 'Quick Learner', date: '', unlocked: false },
    { icon: 'dark_mode', label: 'Night Owl', date: '', unlocked: false },
  ];

  activities: Activity[] = [
    { icon: 'check_circle', color: '#16a34a', text: 'Finished "Data Structures 101"', detail: 'Today, 2:43 PM • Score: 86%' },
    { icon: 'add_circle', color: '#4255FF', text: 'Created "Biology - Finals"', detail: 'Yesterday, 10:20 AM • 43 Cards' },
    { icon: 'group', color: '#7c3aed', text: 'Joined "Python Devs" Class', detail: 'Oct 24, 2023 • 12 Members' },
    { icon: 'military_tech', color: '#f59e0b', text: 'Unlocked "Perfect Score" Badge', detail: 'Oct 23, 2023' },
  ];
}
