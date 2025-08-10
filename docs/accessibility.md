# Accessibility and UX Notes

Keyboard navigation
- Ensure focusable elements in popovers/lightbox have visible focus styles
- Close lightbox with Esc; consider trapping focus while open
- Avoid keyboard traps; make popover dismissible without mouse

Color and contrast
- Use sufficient contrast for labels and text (#111827 on white is OK)
- Consider prefers-color-scheme for dark mode

ARIA and semantics
- Add role="dialog" and aria-modal="true" to the lightbox container
- Add aria-labels to actionable icons if any are added later

Pointer targets
- Ensure touch targets at least 40x40 px where possible

Motion and animation
- Keep transitions subtle and short; provide reduced motion respect if adding larger animations

Images
- Provide meaningful alt text for key images where appropriate (thumbnails are decorative in some contexts)
