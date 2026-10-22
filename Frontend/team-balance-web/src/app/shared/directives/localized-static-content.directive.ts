import { AfterViewInit, Directive, ElementRef, NgZone, OnDestroy, effect, inject } from '@angular/core';
import { LocalizationService } from '../../services/localization.service';

/**
 * Safety net for legacy templates that still contain literal interface text.
 * It deliberately translates only phrases registered in LocalizationService,
 * never arbitrary content entered by agency users.
 */
@Directive({ selector: '[appLocalizedStaticContent]', standalone: true })
export class LocalizedStaticContentDirective implements AfterViewInit, OnDestroy {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly localization = inject(LocalizationService);
  private readonly zone = inject(NgZone);
  private observer?: MutationObserver;

  constructor() {
    effect(() => {
      this.localization.language();
      queueMicrotask(() => this.translateTree());
    });
  }

  ngAfterViewInit(): void {
    this.translateTree();
    this.zone.runOutsideAngular(() => {
      this.observer = new MutationObserver(() => this.translateTree());
      this.observer.observe(this.element.nativeElement, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['placeholder', 'title', 'aria-label'] });
    });
  }

  ngOnDestroy(): void { this.observer?.disconnect(); }

  private translateTree(): void {
    const root = this.element.nativeElement;
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    const nodes: Text[] = [];
    while (walker.nextNode()) nodes.push(walker.currentNode as Text);
    for (const node of nodes) {
      const original = node.nodeValue ?? '';
      const translated = this.localization.traducirTextoEstatico(original);
      if (translated !== original) node.nodeValue = translated;
    }
    root.querySelectorAll<HTMLElement>('[placeholder], [title], [aria-label]').forEach((element) => {
      for (const attribute of ['placeholder', 'title', 'aria-label']) {
        const original = element.getAttribute(attribute);
        if (!original) continue;
        const translated = this.localization.traducirTextoEstatico(original);
        if (translated !== original) element.setAttribute(attribute, translated);
      }
    });
  }
}
