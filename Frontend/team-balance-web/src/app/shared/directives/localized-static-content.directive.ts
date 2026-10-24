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
  private readonly textKeys = new WeakMap<Text, string>();
  private readonly attributeKeys = new WeakMap<HTMLElement, Map<string, string>>();
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
      this.observer = new MutationObserver((mutations) => {
        if (mutations.some((mutation) => !this.isExcludedFromLocalization(mutation.target))) this.translateTree();
      });
      this.observer.observe(this.element.nativeElement, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['placeholder', 'title', 'aria-label'] });
    });
  }

  ngOnDestroy(): void { this.observer?.disconnect(); }

  private translateTree(): void {
    const root = this.element.nativeElement;
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
      acceptNode: (node) => this.isExcludedFromLocalization(node) ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT,
    });
    const nodes: Text[] = [];
    while (walker.nextNode()) nodes.push(walker.currentNode as Text);
    for (const node of nodes) {
      const original = node.nodeValue ?? '';
      const resolvedKey = this.localization.claveContenidoEstatico(original);
      const key = resolvedKey ?? this.textKeys.get(node);
      // Angular can reuse the same Text node for different interpolated values
      // (for example, "Apply filters" and "Updating..."). Prefer the key that
      // matches the node's current value so a temporary loading label is not
      // cached forever.
      if (resolvedKey) this.textKeys.set(node, resolvedKey);
      const translated = key ? this.translatePreservingWhitespace(original, key) : original;
      if (translated !== original) node.nodeValue = translated;
    }
    root.querySelectorAll<HTMLElement>('[placeholder], [title], [aria-label]').forEach((element) => {
      if (this.isExcludedFromLocalization(element)) return;
      for (const attribute of ['placeholder', 'title', 'aria-label']) {
        const original = element.getAttribute(attribute);
        if (!original) continue;
        const keys = this.attributeKeys.get(element) ?? new Map<string, string>();
        const resolvedKey = this.localization.claveContenidoEstatico(original);
        const key = resolvedKey ?? keys.get(attribute);
        if (resolvedKey) {
          keys.set(attribute, resolvedKey);
          this.attributeKeys.set(element, keys);
        }
        const translated = key ? this.translatePreservingWhitespace(original, key) : original;
        if (translated !== original) element.setAttribute(attribute, translated);
      }
    });
  }

  private translatePreservingWhitespace(text: string, key: string): string {
    const leading = text.match(/^\s*/)?.[0] ?? '';
    const trailing = text.match(/\s*$/)?.[0] ?? '';
    return `${leading}${this.localization.traducir(key)}${trailing}`;
  }

  private isExcludedFromLocalization(node: Node): boolean {
    const element = node instanceof Element ? node : node.parentElement;
    return !!element?.closest('[data-localization-ignore], [_echarts_instance_], canvas, svg, .apexcharts-canvas');
  }
}
