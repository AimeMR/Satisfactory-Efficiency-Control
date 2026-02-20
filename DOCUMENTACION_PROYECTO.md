# Documentación del Proyecto: Satisfactory Efficiency Manager

## Contexto del Proyecto
**Satisfactory Efficiency Manager** es una herramienta de escritorio diseñada para optimizar la producción en el juego *Satisfactory*. Permite a los usuarios diseñar diagramas de flujo de fábricas, gestionar máquinas pre-cargadas, agrupar producciones de forma jerárquica y calcular automáticamente la eficiencia y el flujo de los recursos en tiempo real.

El proyecto está construido en **C# / .NET 9** utilizando:
- **WPF (Windows Presentation Foundation)** para la interfaz de usuario.
- **Nodify** para la visualización y gestión del canvas de nodos.
- **Entity Framework Core + SQLite** para la persistencia de datos (máquinas, recetas, etc.).
- **Patrón MVVM** para una separación clara entre la lógica y la interfaz.

---

## Estructura del Proyecto y sus Partes

### 1. Capa de Modelos (`Models/`)
Define las entidades básicas de datos:
- **Machine**: Representa una máquina del juego (Nombre, consumo energético).
- **Recipe**: Define una receta (qué ítems entran y qué ítems salen).
- **Item**: Representa un recurso o producto.
- **RecipeIngredient**: Relación entre recetas e ítems (cantidades).

### 2. Capa de Datos (`Data/` y `Migrations/`)
- **SatisfactoryDbContext**: Configura la conexión a la base de datos SQLite (`satisfactory.db`) y define las tablas.
- **Migrations**: Historial de cambios en la estructura de la base de datos.

### 3. Capa Lógica y Grafo (`Graph/`)
Es el motor del programa:
- **Nodes/MachineNode.cs**: Nodo que representa una máquina física en el canvas. Gestiona sus puertos de entrada/salida.
- **Nodes/FactoryGroupNode.cs**: Nodo contenedor que permite la navegación jerárquica (una fábrica dentro de un grupo).
- **ConnectionModel.cs**: Define el enlace entre un puerto de salida y uno de entrada.
- **FlowCalculator.cs**: Función crítica que recorre el grafo, calcula las proporciones de uso, detecta cuellos de botella y actualiza los flujos de ítems por minuto.

### 4. Capa de Interfaz de Usuario (ViewModels y Views)
#### ViewModels (`ViewModels/`)
- **EditorViewModel**: El "cerebro" de la UI. Gestiona el canvas, la pila de navegación (breadcrumb), los comandos de creación de nodos y la sincronización con el motor de flujo.
- **NodeViewModel / PortViewModel / ConnectionViewModel**: Adaptadores que permiten a Nodify renderizar los modelos del grafo.
- **MachineLibraryViewModel**: Gestiona la lista de máquinas del panel lateral (búsqueda y carga asíncrona).

#### Views (`Views/` y `MainWindow`)
- **MainWindow.xaml**: Define el layout principal (barra de herramientas, breadcrumb, panel lateral replegable y canvas de Nodify).
- **NodeTemplates.xaml**: Diccionario de recursos que define cómo se ven visualmente las máquinas y los puertos.
- **Converters/**: Pequeñas funciones que transforman datos (ej. convertir el flujo en colores para los cables del grafo).

---

## Funciones Generales y su Comportamiento

### Sistema de Navegación Jerárquica
- **Entrar en Grupos**: Al hacer doble clic en un "Grupo de Fábrica", el editor se reinicia para mostrar solo los nodos dentro de ese grupo.
- **Breadcrumb**: Permite ver la ruta actual (ej: Fábrica Principal > Procesado de Hierro) y saltar rápidamente a niveles superiores.
- **Reset de Viewport**: Cada vez que se cambia de nivel, la cámara se centra en el origen (0,0) para evitar desorientación.

### Gestión de Máquinas (Drag & Drop)
- **Biblioteca Lateral**: Las máquinas se cargan desde la base de datos (con una lista de respaldo de 24 máquinas de Satisfactory 1.0 si la base está vacía).
- **Arrastrar y Soltar**: El usuario puede arrastrar máquinas al canvas. El sistema convierte las coordenadas de la pantalla a coordenadas del canvas de Nodify para posicionar la máquina exactamente donde se soltó.

### Cálculo de Flujo y Eficiencia
- **Cálculo Automático**: Cada vez que se crea o elimina una conexión, el `FlowCalculator` recalcula todo el grafo.
- **Visualización de Flujo**: Las conexiones (cables) cambian de color según el ratio de uso (ej: verde si el flujo es óptimo, naranja/rojo si hay saturación o falta de recursos).
- **Ports Dinámicos**: Las máquinas crean automáticamente puertos según los ingredientes de la receta seleccionada.

### Interfaz Colapsable
- El menú lateral de máquinas puede replegarse para maximizar el espacio de trabajo. Un botón "pestaña" persistente permite volver a abrirlo en cualquier momento.
