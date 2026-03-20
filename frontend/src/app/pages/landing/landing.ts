import { Component, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing implements AfterViewInit {
  @ViewChild('modesTrack') modesTrack!: ElementRef<HTMLDivElement>;

  private isAnimating = false;
  private readonly ANIMATION_DURATION = 350;
  private readonly EASING = 'cubic-bezier(0.4, 0, 0.2, 1)';

  constructor(private router: Router) {}

  ngAfterViewInit(): void {
    const track = this.modesTrack.nativeElement;
    track.style.willChange = 'transform';
  }

  private getCardWidth(): number {
    const track = this.modesTrack.nativeElement;
    const card = track.children[0] as HTMLElement;
    if (card) {
      return card.offsetWidth + 20; // card width + gap
    }
    return 300;
  }

  scrollLeft(): void {
    if (this.isAnimating) return;
    this.isAnimating = true;

    const track = this.modesTrack.nativeElement;
    const cards = track.children;
    const cardWidth = this.getCardWidth();

    // Move last card to the beginning
    const lastCard = cards[cards.length - 1];
    track.insertBefore(lastCard, cards[0]);

    // Start from offset position (no transition)
    track.style.transition = 'none';
    track.style.transform = `translateX(-${cardWidth}px)`;

    // Force browser to apply the style
    void track.offsetWidth;

    // Animate to 0
    requestAnimationFrame(() => {
      track.style.transition = `transform ${this.ANIMATION_DURATION}ms ${this.EASING}`;
      track.style.transform = 'translateX(0)';
    });

    setTimeout(() => {
      this.isAnimating = false;
    }, this.ANIMATION_DURATION);
  }

  scrollRight(): void {
    if (this.isAnimating) return;
    this.isAnimating = true;

    const track = this.modesTrack.nativeElement;
    const cards = track.children;
    const cardWidth = this.getCardWidth();

    // Start animation
    track.style.transition = `transform ${this.ANIMATION_DURATION}ms ${this.EASING}`;
    track.style.transform = `translateX(-${cardWidth}px)`;

    setTimeout(() => {
      // Move first card to the end
      const firstCard = cards[0];
      track.appendChild(firstCard);

      // Reset position instantly
      track.style.transition = 'none';
      track.style.transform = 'translateX(0)';

      // Force browser to apply
      void track.offsetWidth;

      this.isAnimating = false;
    }, this.ANIMATION_DURATION);
  }
}
