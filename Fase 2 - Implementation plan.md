# Plan de Implementación - Fase 2: Lógica del Grafo y Motor de Cálculo

Este documento detalla la arquitectura en memoria de la aplicación. Define cómo interactúan las máquinas, las cintas transportadoras y cómo se calculan los flujos de ítems por minuto. Incluye el sistema de **agrupación jerárquica multinivel** como funcionalidad central.

---

## 1. Sistema de Puertos (Pines de Conexión)

Antes de conectar nodos, definimos *dónde* se conectan. Las cintas transportadoras van de un puerto de salida a un puerto de entrada.

* **Tipos:** Puerto de Entrada (`InputPort`) y Puerto de Salida (`OutputPort`).
* **Datos de cada puerto:**
  * Ítem específico que acepta o emite (ej. "Mineral de Hierro").
  * Flujo actual calculado (ítems/minuto) — almacenado *en el puerto* para facilitar su lectura desde la UI.

---

## 2. Jerarquía de Nodos — Agrupación Multinivel

> **Requisito clave:** La agrupación NO es solo un zoom visual. Es una jerarquía real de contextos navegables. Un usuario puede tener, por ejemplo, una fábrica general con 9 materiales finales distintos, y dentro de ella agrupar las 5 máquinas que producen exclusivamente un material en su propio subgrupo. Los grupos pueden anidarse a cualquier profundidad.

### `NodeModel` (Nodo Base)
La plantilla fundamental. Todo nodo tiene:
* `Id` único (GUID).
* `Name` (nombre visible en el lienzo).
* `List<InputPort>` y `List<OutputPort>`.
* Referencia opcional a su **nodo padre** (`ParentNodeId`) para saber en qué contexto vive.

### `MachineNode : NodeModel`
* Enlaza con los datos de Fase 1: qué `Machine` es y qué `Recipe` está usando.
* Modificadores del juego: valor de *Overclock* (por defecto 100%, máximo 250%).

### `FactoryGroupNode : NodeModel` — **Agrupación Multinivel**
El contenedor principal del sistema de profundidad. Su diseño soporta anidamiento arbitrario:

```
FactoryGroupNode (Fábrica General)
│
├── FactoryGroupNode (Línea de Hierro)      ← Grupo de 5 máquinas para Hierro
│   ├── MachineNode (Minero Mk.2)
│   ├── MachineNode (Fundidora x3)
│   └── MachineNode (Constructora)
│
├── FactoryGroupNode (Línea de Cobre)       ← Otro grupo independiente
│   └── ...
│
└── MachineNode (Ensambladora Final)        ← Nodo suelto en el nivel raíz
```

* **Propiedad clave:** `List<NodeModel> Children` — puede contener tanto `MachineNode` como otros `FactoryGroupNode`.
* **Puertos externos:** Los puertos a nivel de grupo son el reflejo automático de las conexiones (`ConnectionModel`) que entran o salen del grupo hacia el exterior. Se generan dinámicamente: cuando una conexión cruza el límite del grupo, se crea/actualiza el puerto externo correspondiente sobre el `FactoryGroupNode`.
* **Navegación:** Doble clic entra al contexto interno. Un breadcrumb (`Fábrica > Línea de Hierro > ...`) permite volver a cualquier nivel anterior.

### `SplitterNode / MergerNode : NodeModel` (Nodos Logísticos)
* Sin receta ni consumo de energía.
* Dividen o unen flujos matemáticamente entre sus puertos.

---

## 3. Sistema de Conexiones (`ConnectionModel`)

Representación de la cinta transportadora o tubería.

* **Mapeo:** `SourcePortId` (OutputPort de origen) → `TargetPortId` (InputPort de destino).
* **Capacidad Máxima:** Simula el nivel de cinta (Mk.1=60/min, Mk.2=120/min, Mk.3=270/min, Mk.4=480/min, Mk.5=780/min). Vital para detectar cuellos de botella.
* **Flujo actual calculado:** Propiedad que `FlowCalculator` actualiza en cada pasada. Permite que la UI coloree la cinta (verde=OK, naranja=cerca del límite, rojo=saturada).
* **Cruce de grupo:** Flag `IsCrossBoundary` que indica si la conexión cruza el perímetro de un `FactoryGroupNode`. Usado para generar los puertos externos automáticos del grupo.

---

## 4. El Motor de Cálculo (`FlowCalculator`)

El "cerebro" de la aplicación. Se dispara cada vez que el usuario conecta un cable, cambia una receta o modifica un overclock.

### Paso 0 — Reseteo
Pone todos los flujos de puertos y conexiones a 0 antes de recalcular.

### Paso 1 — Detección de Ciclos ⚠️
Antes de propagar, el calculador ejecuta una búsqueda de ciclos (DFS con marcado de estado) sobre el grafo completo.
* Si detecta un ciclo, **marca los nodos involucrados** con un aviso de error y **detiene el cálculo** en ese subgrafo para no entrar en un bucle infinito.
* Los ciclos son raros en Satisfactory pero posibles si el usuario los dibuja por error.

### Paso 2 — Generación (Nodos Fuente)
Busca todos los nodos sin puertos de entrada activos (mineros, extractores, pozos de agua).
* Calcula su producción según pureza del nodo de recurso y valor de overclock.
* Asigna el flujo calculado a sus puertos de salida.

### Paso 3 — Propagación (Orden Topológico)
Recorre el grafo en orden topológico (algoritmo de Kahn o DFS post-order) siguiendo las conexiones.
* Propaga el flujo de cada conexión desde el puerto de salida al puerto de entrada destino.
* **Detección de cuello de botella:** Si el flujo supera la `CapacidadMáxima` de la conexión, se satura. La conexión transmite solo el máximo permitido y se marca como `Bottleneck = true`.

### Paso 4 — Evaluación de Eficiencia por Máquina
Al llegar el flujo a un `MachineNode`, compara lo que entra con lo que pide la receta para **cada ingrediente por separado**.

* **Múltiples ingredientes (caso crítico):** Una Ensambladora puede requerir 2 materiales distintos. El calculador evalúa el porcentaje de cumplimiento de *cada* ingrediente de forma independiente. El **ingrediente más restrictivo** (el de menor porcentaje relativo) determina el ritmo real de producción.

  > Ejemplo: Receta pide 30 Hierro/min y 20 Cobre/min. Llegan 30 Hierro pero solo 10 Cobre (50%). La máquina produce al **50%** de su configuración de overclock.

* **Si entra menos de lo necesario:** Se calcula el porcentaje de *Starvation* y la producción de salida se reduce proporcionalmente.
* **Si entra lo correcto o más:** La máquina produce al 100% de su overclock y empuja el resultado al siguiente nodo.

### Paso 5 — Evaluación de Grupos
Tras calcular todos los nodos hijos, el calculador sube un nivel y recalcula los puertos externos del `FactoryGroupNode` para que la vista del nivel padre refleje los flujos correctos que entran y salen del grupo.

---

## 5. Estructura de Carpetas Propuesta

```
SatisfactoryManagerApp/
├── Models/               ← Fase 1 (ya completada)
│   ├── Item.cs
│   ├── Machine.cs
│   ├── Recipe.cs
│   └── RecipeIngredient.cs
│
└── Graph/                ← Fase 2 (nueva carpeta)
    ├── Ports/
    │   ├── InputPort.cs
    │   └── OutputPort.cs
    ├── Nodes/
    │   ├── NodeModel.cs
    │   ├── MachineNode.cs
    │   ├── FactoryGroupNode.cs
    │   ├── SplitterNode.cs
    │   └── MergerNode.cs
    ├── ConnectionModel.cs
    └── FlowCalculator.cs
```

---

## 6. Orden de Implementación

| Orden | Clase | Dependencias |
|-------|-------|--------------|
| 1 | `InputPort`, `OutputPort` | Ninguna |
| 2 | `NodeModel` | Puertos |
| 3 | `MachineNode`, `FactoryGroupNode`, `SplitterNode`, `MergerNode` | `NodeModel` + Modelos de Fase 1 |
| 4 | `ConnectionModel` | Puertos |
| 5 | `FlowCalculator` | Todos los anteriores |

---

## 7. Plan de Verificación

* [ ] Crear un grafo sencillo en código (2 máquinas, 1 conexión) y verificar que `FlowCalculator` produce el flujo correcto.
* [ ] Verificar detección de cuello de botella: forzar flujo > capacidad de cinta y comprobar que `Bottleneck = true`.
* [ ] Verificar *Starvation* con una máquina de 1 ingrediente con flujo insuficiente.
* [ ] Verificar *Starvation* con una máquina de 2 ingredientes con el caso del ingrediente más restrictivo.
* [ ] Verificar detección de ciclos: crear un grafo circular y comprobar que el calculador no entra en bucle.
* [ ] Verificar agrupación multinivel: crear un `FactoryGroupNode` anidado dentro de otro y comprobar que los puertos externos del padre se generan correctamente.