# Shadow Beat: Fragmentos de Luz

Version jugable en Unity para un juego de plataformas ritmico / arcade de precision.

## Que incluye este MVP

- Menu principal estilo arcade neon usando la foto PNG del escenario como fondo, con botones reales encima para seleccion de nivel, seleccion de forma y `PLAY NOW`.
- 7 niveles principales: Bosque Neon, Ruinas Cyber y Ciudad Invertida.
- Lux avanza automaticamente hacia adelante.
- Salto con `Space` o click.
- Formas: cubo, esfera, nave, sombra y rayo de luz.
- Color aleatorio de Lux al iniciar o reintentar.
- Selector de forma inicial de Lux: Esfera, Cubo, Piramide o Diamante.
- Portales de transformacion y de gravedad, con cambio visual del fondo.
- Ambientacion inspirada en ciudad futurista nocturna: skyline oscuro, camino cyber, luces cyan/naranja/magenta, carteles flotantes y meta como nucleo luminoso.
- Obstaculos, barreras de sombra, enemigos moviles y plataformas moviles.
- Pinchos triangulares naranja neon y cristales coleccionables con forma de rombo luminoso.
- Checkpoint y reintento rapido.
- Meta de nivel, panel de victoria, boton de siguiente nivel y volver al menu.
- UI con nombre de nivel, progreso, cristales, intentos y puntuacion.
- Guardado simple de nivel completado, mejor puntuacion y progreso desbloqueado con `PlayerPrefs`.

## Como crear el juego

1. Abrir esta carpeta como proyecto Unity.
2. Salir de Play Mode si esta activo.
3. En el menu superior elegir `Shadow Beat > Create Complete Game`.
4. Abrir `Assets/Scenes/MainMenu.unity`.
5. Apretar Play.
6. Elegir nivel, elegir forma inicial y presionar `PLAY NOW`.

Si las escenas ya estaban abiertas antes de cambiar el proyecto, volver a ejecutar `Shadow Beat > Create Complete Game` para regenerarlas con la ambientacion nueva.

## Controles

- `Space` o click: saltar / activar accion principal.
- En forma nave, mantener `Space` o click para elevarse.

## Alcance dejado para etapas posteriores

- Musica sincronizada real por beat.
- Animaciones y particulas finales.
- Skins desbloqueables.
- Jefes como Noctis.
- Arte final de personajes y escenarios.
