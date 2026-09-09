# Propuesta de tecnología de UI para la migración del IDE

## 1. Objetivo

La fase actual de migración prioriza estabilizar el runtime y el build bajo .NET 8 LTS. La UI no debe bloquear esa transición, pero sí debe prepararse con una tecnología que permita mantener una apariencia coherente entre sistemas de escritorio y reducir el acoplamiento con Gtk# y Mono.

## 2. Decisión principal

Se recomienda evaluar y, en su momento, adoptar Avalonia UI como base para la nueva capa de UI del IDE.

### ¿Por qué Avalonia?

- Es nativo a .NET y compatible con .NET 8 LTS.
- Permite construir interfaces cross-platform con un único modelo de UI.
- Tiene un enfoque moderno y activo de comunidad.
- Soporta XAML y un modelo de composición muy parecido a WPF, lo que ayuda a migrar mentalmente una arquitectura de IDE desktop.
- Mejora la consistencia visual entre Linux, Windows y macOS sin depender del stack Gtk# clásico.
- Es más adecuado para un IDE moderno que un enfoque heredado de bindings de GTK.

## 3. Criterios de selección

La tecnología final debe cumplir, al menos, estos requisitos:

- Compatibilidad con .NET 8.
- Soporte multiplataforma real para escritorio.
- Consistencia visual entre entornos.
- Buen modelo de diseño y composición para un IDE complejo.
- Facilidad para desacoplar la capa de UI de la lógica del producto.
- Capacidad de migrarse por módulos y no por reescritura total.

## 4. Opciones evaluadas y comparación

### Opción A: Avalonia UI (recomendada)

Ventajas:
- UI de escritorio moderna y cross-platform.
- Reutilizable para Linux, Windows y macOS.
- Buen soporte de MVVM y diseño basado en XAML.
- No depende del runtime Mono/Gtk# heredado.
- Permite una migración gradual por paneles y vistas.

Desventajas:
- Requiere reestructura de patrones de UI actuales.
- Habrá que adaptar widgets y comportamientos del IDE a la nueva plataforma.
- La migración completa no es instantánea ni trivial.

### Opción B: MAUI / .NET desktop moderno

Ventajas:
- Excelente soporte de .NET moderno.
- Gran integración con la plataforma .NET actual.

Desventajas:
- No es la mejor opción para un IDE de escritorio complejo con necesidades de consistencia cross-platform visual y de composición.
- Menos alineado con el tipo de shell de herramientas y editores de código que necesita un IDE.
- Puede requerir más trabajo de adaptación para una experiencia de editor nativa y detallada.

### Opción C: Eto.Forms

Ventajas:
- Cross-platform y ligero.
- Fija una base viable para UI consistente.

Desventajas:
- Tiene un estilo visual menos “premium” y menos alineado con un IDE moderno.
- Menos escalable para una experiencia visual compleja y custom UI.
- Menor atractivo para una aplicación de edición y herramientas avanzadas.

## 5. Recomendación de arquitectura

La migración de UI debe seguir un modelo de desacoplamiento estrictamente por capas:

- Capa de dominio y negocio: sin referencias a Gtk# ni a Mono.
- Capa de servicios del IDE: lógica de edición, proyectos, compilación y extensibilidad.
- Capa de plataforma: adaptadores de sistema operativo, archivos, shell, servicios y accesos del entorno.
- Capa de UI: vistas, panels, docking, editor visual y shell principal.

Este modelo permite que el IDE se migre a una UI nueva sin romper la base de negocio ya modernizada.

## 6. Plan de adopción recomendado

### Fase A: Preparación de la capa de UI

- Definir interfaces para shell, dialogos, vistas, servicios del entorno y acciones del usuario.
- Aislar la lógica del IDE de cualquier dependencia directa de Gtk#.
- Mantener un adaptador temporal para compatibilidad con la base actual.

### Fase B: Prototipo de shell base

- Migrar la shell principal a un modelo moderno de docking y ventanas.
- Validar estilos, color, spacing y comportamiento visual general.
- Revisar performance y carga de paneles.

### Fase C: Migración gradual por vistas

- Portar las vistas de proyecto, explorador, solución y paneles de salida.
- Dejar la edición principal para el final, tras validar la infraestructura base.
- Mantener compatibilidad con las extensiones del IDE durante el proceso.

### Fase D: Cutover final

- Sustituir el shell heredado.
- Validar la experiencia visual y la operatividad en Linux.
- Luego preparar la extensión a Windows y macOS con la misma base arquitectónica.

## 7. Riesgos a considerar

- Reescritura parcial de la experiencia visual del IDE.
- Diferencias de comportamiento nativo entre plataformas.
- Coste de adaptación de widgets y temas personalizados.
- Aumenta la necesidad de una capa de maqueta/abstracción para no acoplar toda la app a la UI.

## 8. Conclusión

La mejor opción para el objetivo del repositorio es avanzar con Avalonia UI como plataforma de UI principal para la etapa moderna del IDE, dejando el Gtk# heredado como adaptación temporal para la estabilización del sistema y no como base final.

La migración de UI debe realizarse después de estabilizar runtime y build, pero con una arquitectura preparada para ello desde ya. Esto permite evitar un bloqueo prematuro en la decisión visual y dejar la base del producto lista para una evolución moderna, consistente y multiplataforma.

## 9. Referencia operativa

Este documento debe leerse junto con:

- `migration-plan.md`
- `migration-execution-plan.md`
- `phase-1-stabilization-plan.md`

Estas referencias conservan el contexto de la migración técnica y permiten continuar el trabajo en etapas sin perder la estrategia general.
