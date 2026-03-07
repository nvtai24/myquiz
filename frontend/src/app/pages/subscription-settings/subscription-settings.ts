import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-subscription-settings',
  standalone: true,
  imports: [],
  templateUrl: './subscription-settings.html',
  styleUrl: './subscription-settings.css',
})
export class SubscriptionSettings {
  currentPlan = signal<'free' | 'pro'>('free');

  plans = [
    {
      id: 'free' as const,
      name: 'Free Plan',
      price: '$0',
      period: '/month',
      features: [
        { text: 'Up to 20 study sets', included: true },
        { text: 'Basic AI generation', included: true },
        { text: 'Community support', included: true },
        { text: 'Unlimited AI generation', included: false },
        { text: 'Advanced analytics', included: false },
        { text: 'Priority support', included: false },
        { text: 'Custom themes', included: false },
      ],
    },
    {
      id: 'pro' as const,
      name: 'Pro Plan',
      price: '$9.99',
      period: '/month',
      popular: true,
      features: [
        { text: 'Unlimited study sets', included: true },
        { text: 'Unlimited AI generation', included: true },
        { text: 'Advanced analytics', included: true },
        { text: 'Priority support', included: true },
        { text: 'Custom themes', included: true },
        { text: 'Export to PDF', included: true },
        { text: 'Collaboration tools', included: true },
      ],
    },
    {
      id: 'pro' as const,
      name: 'Pro Plan',
      price: '$9.99',
      period: '/month',
      popular: true,
      features: [
        { text: 'Unlimited study sets', included: true },
        { text: 'Unlimited AI generation', included: true },
        { text: 'Advanced analytics', included: true },
        { text: 'Priority support', included: true },
        { text: 'Custom themes', included: true },
        { text: 'Export to PDF', included: true },
        { text: 'Collaboration tools', included: true },
      ],
    },
  ];

  paymentMethods = signal([
    { type: 'visa', last4: '4242', expiry: '12/25', isDefault: true },
    { type: 'mastercard', last4: '8888', expiry: '06/26', isDefault: false },
  ]);

  billingHistory = signal([
    { date: 'Dec 1, 2024', description: 'Pro Plan — Monthly', amount: '$9.99', status: 'Paid' as const },
    { date: 'Nov 1, 2024', description: 'Pro Plan — Monthly', amount: '$9.99', status: 'Paid' as const },
    { date: 'Oct 1, 2024', description: 'Pro Plan — Monthly', amount: '$9.99', status: 'Paid' as const },
    { date: 'Sep 1, 2024', description: 'Pro Plan — Monthly', amount: '$9.99', status: 'Paid' as const },
  ]);

  selectPlan(planId: 'free' | 'pro') {
    this.currentPlan.set(planId);
  }

  removePayment(index: number) {
    this.paymentMethods.update(list => list.filter((_, i) => i !== index));
  }

  setDefaultPayment(index: number) {
    this.paymentMethods.update(list =>
      list.map((m, i) => ({ ...m, isDefault: i === index }))
    );
  }
}
