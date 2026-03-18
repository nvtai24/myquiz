import { Component, PLATFORM_ID, inject, signal, OnInit } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface QuizAttemptSummary {
  id: string;
  deckId: string;
  deckTitle: string;
  score: number;
  totalCorrect: number;
  totalQuestions: number;
  timeTaken: string;
  date: string;
}

@Component({
  selector: 'app-quiz-history',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './quiz-history.html',
  styleUrl: './quiz-history.css',
})
export class QuizHistory implements OnInit {
  private platformId = inject(PLATFORM_ID);
  
  history = signal<QuizAttemptSummary[]>([]);
  
  ngOnInit() {
    this.loadAllHistory();
  }

  loadAllHistory() {
    if (!isPlatformBrowser(this.platformId)) return;
    
    const allAttempts: QuizAttemptSummary[] = [];
    
    // Iterate over all localStorage items looking for 'quiz_history_' keys
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith('quiz_history_')) {
        try {
          const arr = JSON.parse(localStorage.getItem(key) || '[]');
          if (Array.isArray(arr)) {
            allAttempts.push(...arr);
          }
        } catch (e) {
          // Ignore parse errors
        }
      }
    }
    
    // Sort by date descending
    allAttempts.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
    
    this.history.set(allAttempts);
  }

  formatDate(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  getScoreColor(score: number): string {
    if (score >= 80) return '#16a34a';
    if (score >= 50) return '#d97706';
    return '#ef4444';
  }
}
