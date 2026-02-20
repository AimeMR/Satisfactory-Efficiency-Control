# Plan de Implementación: Gestor de Eficiencia para Satisfactory

Este documento detalla la hoja de ruta para el desarrollo de la aplicación de escritorio en C# (WPF/Avalonia) para la gestión y cálculo de fábricas en *Satisfactory*.

---

## Fase 1: Estructura de la Base de Datos (SQLite)

Antes de pintar nodos, necesitas los datos crudos de *Satisfactory*. Crea tu proyecto en C#, instala los paquetes NuGet de EF Core y SQLite, y define tus modelos.

* **Objetivo:** Tener una base de datos local y ligera que el programa pueda consultar al instante.
* **Acciones:**
  * Configurar Entity Framework Core con SQLite.
  * Crear modelos para Items (materiales), Máquinas (con su consumo de energía) y Recetas (entradas, salidas y tiempos).

---

## Fase 2: Lógica del Grafo y Motor de Cálculo (Backend)

Aquí es donde entra tu lógica pura en C#. Debes crear las clases que representarán tu fábrica en memoria antes de dibujarla.

* **Clase `NodeModel`:** Representa una entidad en tu lienzo. Puede ser de tipo `MachineNode` (una Ensambladora) o `FactoryGroupNode` (el contenedor general).
* **Clase `ConnectionModel`:** Representa las cintas transportadoras. Tiene un `SourceNode` (salida) y un `TargetNode` (entrada), además de la propiedad de "ítems por minuto".
* **Calculadora de Flujos:** Un servicio estático o clase gestora que recorre la lista de `ConnectionModel`. Si una máquina produce 30 placas/minuto, este servicio actualiza el valor de la conexión y verifica si la máquina destino está saturada o carente de materiales.

---

## Fase 3: Interfaz Gráfica y Nodos (Frontend)

Una vez que la lógica funciona por debajo, toca mostrarla usando tu framework (WPF/Avalonia) y la librería de nodos elegida (ej. Nodify).

* **El Lienzo Principal:** Agrega el control `NodifyEditor` (o similar) a tu ventana principal. Este control ya trae el zoom, el paneo y la cuadrícula de fondo integrados.
* **Diseño del Nodo:** Crea un archivo `.xaml` (`DataTemplate`) para decirle al programa cómo se ve un `MachineNode`. Aquí pondrás el texto del nombre de la máquina, los íconos de los materiales y los "pines" (puntos de anclaje) de entrada y salida.
* **Data Binding:** Conecta la lista de `NodeModel` de tu backend con el lienzo interactivo. Cada vez que agregues un nodo por código, aparecerá en la pantalla automáticamente.

---

## Fase 4: La Funcionalidad de "Profundidad" (Sub-fábricas)

Este es el requisito estrella: poder agrupar máquinas en una fábrica general y hacer zoom hacia adentro para ver el detalle.

* **Lógica de agrupación:** Un `FactoryGroupNode` simplemente contiene una propiedad interna de tipo `List<NodeModel>` (los nodos hijos que viven dentro de esa fábrica).
* **Navegación:** Configura un evento de doble clic en la interfaz sobre un `FactoryGroupNode`.
* **Cambio de contexto:** Cuando el usuario hace doble clic, el editor visual cambia su fuente de datos. En lugar de mostrar el mapa general, pasa a mostrar la lista interna del nodo seleccionado. 
* **Retorno:** Añadir un botón de "Volver atrás" en la interfaz superior para regresar al nivel de la fábrica principal (el nodo padre).