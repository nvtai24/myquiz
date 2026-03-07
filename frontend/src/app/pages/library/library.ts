import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

export interface Deck {
  id: string;
  name: string;
  description: string;
  visibility: string;
  status: string;
  tags: string[];
  questionCount: number;
  thumbnailUrl: string;
  createdAt: string;
  updatedAt: string | null;
  // UI-specific mock properties to keep rating and visuals
  rating?: number;
  reviews?: string;
  color?: string;
  icon?: string;
}

@Component({
  selector: 'app-library',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './library.html',
  styleUrl: './library.css',
})
export class Library {
  activeTab = signal<'all' | 'created' | 'studying' | 'folders'>('all');
  searchQuery = signal('');
  minRating = signal(1);
  sortBy = signal('recent');
  isSortOpen = signal(false);

  // Pagination
  currentPage = signal(1);
  itemsPerPage = signal(2); // Set low for demo purposes based on max 3 items

  allDecks: Deck[] = [
    {
        "id": "6860a3d1-3aa8-431a-9ee8-468df0156169",
        "name": "ASP.NET Core and RESTful Services Study Deck",
        "description": "This deck covers key concepts, principles, and components of ASP.NET Core and RESTful web services.",
        "visibility": "Private",
        "status": "Draft",
        "tags": [
            "ASP.NET Core",
            "REST",
            "Web Services",
            "HTTP",
            "API"
        ],
        "questionCount": 18,
        "thumbnailUrl": "",
        "createdAt": "2026-02-11T17:57:26.539134Z",
        "updatedAt": null,
        rating: 4.8,
        reviews: "1.2k",
        color: "#64748b",
        icon: "api"
    },
    {
        "id": "07ac6ec4-bb55-465f-b2b0-4c1134b5df5e",
        "name": "ASP.NET Core and Angular 2 Study Deck",
        "description": "This deck covers key concepts and components of ASP.NET Core and Angular 2 as discussed in Valerio De Sanctis's book.",
        "visibility": "Private",
        "status": "Draft",
        "tags": [
            "ASP.NET Core",
            "Angular 2",
            "Web Development"
        ],
        "questionCount": 380,
        "thumbnailUrl": "",
        "createdAt": "2026-02-10T17:19:47.075178Z",
        "updatedAt": null,
        rating: 4.9,
        reviews: "850",
        color: "#7c3aed",
        icon: "code"
    },
    {
        "id": "87e1a664-5d0e-4b06-b912-cc9ef4ac0634",
        "name": "ASP.NET Core 1.1 Web API Basics",
        "description": "A study deck covering the fundamentals of ASP.NET Core 1.1 Web API, including setup, middleware, controllers, and data management.",
        "visibility": "Private",
        "status": "Draft",
        "tags": [
            "ASP.NET Core",
            "Web API",
            "MVC",
            "Programming",
            "Development"
        ],
        "questionCount": 178,
        "thumbnailUrl": "",
        "createdAt": "2026-02-10T17:15:50.269887Z",
        "updatedAt": null,
        rating: 4.7,
        reviews: "430",
        color: "#059669",
        icon: "webhook"
    }
  ];

  get filteredSets(): Deck[] {
    let decks = this.allDecks.filter(s => {
      // Filter by search query
      if (this.searchQuery()) {
        const q = this.searchQuery().toLowerCase();
        if (!s.name.toLowerCase().includes(q) && !s.description.toLowerCase().includes(q)) {
          return false;
        }
      }
      // Filter by minimum rating
      if ((s.rating || 0) < this.minRating()) return false;
      return true;
    });

    // Sort
    decks.sort((a, b) => {
      if (this.sortBy() === 'recent') {
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      }
      if (this.sortBy() === 'rating') {
        return (b.rating || 0) - (a.rating || 0);
      }
      if (this.sortBy() === 'terms') {
        return b.questionCount - a.questionCount;
      }
      return 0;
    });

    return decks;
  }

  get totalPages(): number {
    return Math.ceil(this.filteredSets.length / this.itemsPerPage());
  }

  get paginatedSets(): Deck[] {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    const end = start + this.itemsPerPage();
    return this.filteredSets.slice(start, end);
  }

  setTab(tab: 'all' | 'created' | 'studying' | 'folders') {
    this.activeTab.set(tab);
    this.currentPage.set(1);
  }

  toggleSort() {
    this.isSortOpen.update(v => !v);
  }

  setSort(sortType: string) {
    this.sortBy.set(sortType);
    this.isSortOpen.set(false);
    this.currentPage.set(1);
  }

  clearFilters() {
    this.searchQuery.set('');
    this.minRating.set(1);
    this.sortBy.set('recent');
    this.currentPage.set(1);
  }

  onSearchChange(query: string) {
    this.searchQuery.set(query);
    this.currentPage.set(1);
  }

  onMinRatingChange(rating: number) {
    this.minRating.set(rating);
    this.currentPage.set(1);
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage.set(page);
    }
  }
}
