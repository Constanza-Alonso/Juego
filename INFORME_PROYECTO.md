# Informe del Proyecto

## Titulo

**Shadow Beat: Fragmentos de Luz**

## Resumen general

Shadow Beat: Fragmentos de Luz es un videojuego 2D desarrollado en Unity. El proyecto corresponde a un juego de plataformas ritmico / arcade de precision, inspirado en la logica de avance automatico de juegos como Geometry Dash, pero adaptado a una propuesta propia con historia, personaje principal, antagonista, mundos diferenciados, niveles progresivos, cristales coleccionables, transformaciones y sistema de puntuacion.

El jugador controla a Lux, una figura geometrica luminosa que avanza automaticamente por escenarios oscuros. La jugabilidad se basa en reaccionar a obstaculos, saltar en el momento correcto, atravesar portales, cambiar de forma, invertir la gravedad y llegar a la meta de cada nivel.

El proyecto fue implementado como una version funcional inicial, priorizando que existan las mecanicas principales y una estructura clara para continuar el desarrollo en etapas posteriores.

## Motor seleccionado

El motor seleccionado para el desarrollo es **Unity 2022.3 LTS**.

Unity fue elegido porque permite trabajar con:

- escenas 2D,
- fisicas y colisiones,
- componentes reutilizables,
- scripts en C#,
- UI para menus y HUD,
- generacion de niveles desde el editor,
- soporte para exportar el proyecto a distintas plataformas.

Aunque existen alternativas mas simples como PyGame o mas livianas como Godot, Unity ofrece mejores herramientas integradas para escalar el proyecto hacia una version mas completa.

## Genero del videojuego

El videojuego pertenece al genero de **plataformas ritmico / arcade de precision**.

Tambien puede clasificarse como:

- juego de plataformas,
- juego de reflejos,
- juego de habilidad,
- juego de avance automatico,
- juego casual con dificultad progresiva.

## Historia principal

El juego ocurre en **Umbra**, un mundo que anteriormente estaba iluminado por un gran cristal central conocido como **El Nucleo de Luz**.

Durante mucho tiempo, este nucleo mantuvo el equilibrio entre la luz y la oscuridad. Sin embargo, una entidad oscura llamada **Noctis** destruyo el nucleo y disperso sus fragmentos por distintos territorios.

Desde ese momento, Umbra quedo cubierta por sombras, trampas y criaturas oscuras. Lux, una chispa nacida del ultimo fragmento de luz, debe recorrer los distintos niveles para recuperar los fragmentos perdidos y restaurar la luz del mundo.

## Personaje principal

### Lux

Lux es el protagonista del juego. Es una figura geometrica luminosa que representa la esperanza dentro de Umbra.

Caracteristicas:

- avanza automaticamente hacia adelante,
- no habla,
- expresa su identidad mediante brillo, color y movimiento,
- puede transformarse al atravesar portales,
- puede saltar, volar, rebotar, atravesar sombras o moverse a alta velocidad segun su forma.

## Antagonista

### Noctis

Noctis es la entidad oscura que destruyo el Nucleo de Luz.

En esta etapa del proyecto Noctis todavia no aparece como jefe final jugable, pero esta representado conceptualmente por:

- escenarios oscuros,
- obstaculos,
- trampas,
- barreras de sombra,
- enemigos moviles,
- aumento progresivo de dificultad.

## Mundos y zonas

El mundo de Umbra esta dividido en tres zonas principales implementadas mediante niveles:

### Bosque Oscuro

Niveles:

- Nivel 1: Primer destello.
- Nivel 2: Camino de sombras.
- Nivel 3: El salto perdido.

Funcion:

- introducir al jugador en los controles basicos,
- presentar saltos simples,
- mostrar cristales coleccionables,
- introducir barreras de sombra y transformaciones iniciales.

### Ruinas de Cristal

Niveles:

- Nivel 4: Fragmentos rotos.
- Nivel 5: Torres caidas.
- Nivel 6: Cristal inestable.

Funcion:

- aumentar la dificultad,
- introducir la forma nave,
- agregar plataformas moviles,
- usar enemigos moviles,
- introducir la forma rayo.

### Ciudad Invertida

Nivel:

- Nivel 7: Gravedad cero.

Funcion:

- introducir cambios de gravedad,
- obligar al jugador a adaptarse a caminar por el techo,
- cerrar la progresion principal del juego.

## Niveles implementados

El proyecto incluye siete niveles principales:

| Numero | Nombre | Mundo | Mecanica destacada |
|---|---|---|---|
| 1 | Primer destello | Bosque Oscuro | Tutorial, salto y cristales |
| 2 | Camino de sombras | Bosque Oscuro | Forma sombra y barrera oscura |
| 3 | El salto perdido | Bosque Oscuro | Forma esfera |
| 4 | Fragmentos rotos | Ruinas de Cristal | Forma nave |
| 5 | Torres caidas | Ruinas de Cristal | Plataformas moviles y enemigo movil |
| 6 | Cristal inestable | Ruinas de Cristal | Forma rayo |
| 7 | Gravedad cero | Ciudad Invertida | Cambio de gravedad |

## Mecanicas principales implementadas

### Avance automatico

Lux se mueve automaticamente hacia adelante. El jugador no controla la direccion horizontal, sino que debe accionar en el momento correcto para sobrevivir.

Script principal:

- `LuxController.cs`

### Salto

El salto es la accion principal del jugador.

Controles:

- tecla `Space`,
- click del mouse.

Se agrego un pequeno margen de tolerancia mediante:

- `jumpBufferTime`,
- `coyoteTime`.

Esto permite que el salto sea mas amable y no dependa de una precision extrema en la primera version del juego.

### Cambio de gravedad

El juego incluye portales capaces de invertir la gravedad. Cuando Lux atraviesa uno de estos portales, puede desplazarse por el techo.

Tambien se corrigio el sistema para que al atravesar un portal de cubo la gravedad vuelva a su estado normal, evitando que el jugador quede permanentemente arriba sin poder bajar.

Scripts relacionados:

- `Portal.cs`,
- `PortalType.cs`,
- `LuxController.cs`.

### Transformaciones

Lux puede cambiar de forma segun el portal que atraviese.

Formas implementadas:

- Cubo: forma basica con salto normal.
- Esfera: invierte/rebota cambiando la direccion de movimiento vertical.
- Nave: permite volar manteniendo la accion.
- Sombra: permite atravesar barreras oscuras.
- Rayo: aumenta la velocidad.

Scripts relacionados:

- `LuxForm.cs`,
- `PortalType.cs`,
- `Portal.cs`,
- `LuxController.cs`.

### Obstaculos

Los obstaculos provocan la muerte del jugador y activan el sistema de reintento.

Obstaculos implementados:

- pinchos,
- enemigos moviles,
- barreras de sombra,
- caida al vacio mediante zona de muerte.

Scripts relacionados:

- `Hazard.cs`,
- `MovingHazard.cs`,
- `ShadowBarrier.cs`,
- `DeathZone.cs`.

### Cristales coleccionables

Los cristales son objetos opcionales que aumentan la puntuacion final del nivel.

Script:

- `CrystalCollectible.cs`.

### Checkpoints y reintentos

El sistema de reintento permite volver al ultimo checkpoint o al inicio del nivel despues de morir.

Scripts:

- `Checkpoint.cs`,
- `LevelManager.cs`.

### Meta de nivel

Cada nivel tiene una meta. Al tocarla, se marca el nivel como completado, aparece un panel de victoria y se guarda el progreso.

Script:

- `Goal.cs`,
- `LevelManager.cs`.

### Mensaje de finalizacion

Al terminar un nivel se muestra el mensaje:

**JUEGO COMPLETADO**

El panel tambien incluye opciones para:

- avanzar al siguiente nivel,
- reintentar,
- volver al menu.

## Menu principal

El proyecto incluye una escena de menu principal llamada:

- `MainMenu.unity`

Desde el menu se pueden elegir los siete niveles del juego.

Scripts relacionados:

- `MainMenuController.cs`,
- `LevelSelectButton.cs`.

## Interfaz de usuario

Durante el nivel se muestra una UI simple con:

- nombre del nivel,
- porcentaje de progreso,
- cantidad de cristales obtenidos,
- numero de intentos,
- puntuacion,
- boton para volver al menu.

Script:

- `ShadowBeatUI.cs`.

## Sistema de puntuacion

La puntuacion considera:

- porcentaje completado,
- cristales recolectados,
- cantidad de intentos,
- bonificacion por completar sin morir.

La mejor puntuacion se guarda con `PlayerPrefs`.

Script:

- `LevelManager.cs`.

## Guardado de progreso

El proyecto utiliza `PlayerPrefs` para guardar:

- niveles completados,
- mejor puntuacion,
- ultimo nivel desbloqueado.

Esto permite extender el menu en futuras etapas para bloquear/desbloquear niveles o mostrar medallas.

## Generacion de escenas

El proyecto incluye un generador dentro del editor de Unity:

- `ShadowBeatSceneCreator.cs`

Menu en Unity:

- `Shadow Beat > Create Complete Game`

Este generador crea:

- `MainMenu.unity`,
- los 7 niveles,
- UI,
- camaras,
- fondos,
- plataformas,
- obstaculos,
- portales,
- cristales,
- metas,
- zonas de muerte,
- configuracion de Build Settings.

## Archivos principales del proyecto

### Scripts de gameplay

- `LuxController.cs`: controla movimiento, salto, gravedad y formas de Lux.
- `Portal.cs`: aplica transformaciones o cambios de gravedad.
- `Hazard.cs`: elimina al jugador al tocar obstaculos.
- `DeathZone.cs`: reinicia al jugador si cae al vacio.
- `CrystalCollectible.cs`: gestiona cristales recolectables.
- `Checkpoint.cs`: actualiza el punto de reaparicion.
- `Goal.cs`: detecta el final de nivel.
- `LevelManager.cs`: administra intentos, score, progreso, reinicio y finalizacion.

### Scripts de UI y menu

- `ShadowBeatUI.cs`: actualiza HUD y panel de victoria.
- `MainMenuController.cs`: carga niveles desde el menu.
- `LevelSelectButton.cs`: conecta cada boton con su escena.

### Scripts de elementos dinamicos

- `MovingPlatform.cs`: mueve plataformas.
- `MovingHazard.cs`: mueve enemigos/obstaculos.
- `ShadowBarrier.cs`: permite pasar solo en forma sombra.

### Editor

- `ShadowBeatSceneCreator.cs`: genera el juego completo desde Unity.

## Estado actual del desarrollo

El proyecto se encuentra en una version funcional inicial. Ya se puede:

- abrir un menu,
- elegir niveles,
- jugar los siete niveles,
- completar niveles,
- ver mensaje de finalizacion,
- volver al menu,
- recoger cristales,
- sumar puntos,
- morir y reintentar.

## Limitaciones actuales

Quedan pendientes para una etapa posterior:

- arte final del personaje,
- sprites y animaciones completas,
- musica sincronizada al ritmo,
- efectos de sonido,
- particulas,
- skins desbloqueables reales,
- jefes como Noctis,
- niveles mas largos y pulidos manualmente,
- pantalla de creditos,
- ajustes finos de dificultad.

## Instrucciones para ejecutar

1. Abrir el proyecto en Unity 2022.3 LTS.
2. Salir de Play Mode si esta activo.
3. Ejecutar `Shadow Beat > Create Complete Game`.
4. Abrir `Assets/Scenes/MainMenu.unity`.
5. Presionar Play.
6. Elegir un nivel desde el menu.

## Conclusion

Shadow Beat: Fragmentos de Luz ya cuenta con una base funcional para presentar el concepto del videojuego. La version actual implementa las mecanicas principales, la progresion por niveles, el menu de seleccion y el sistema de finalizacion. El proyecto esta preparado para continuar con una etapa de pulido visual, sonoro y narrativo.
