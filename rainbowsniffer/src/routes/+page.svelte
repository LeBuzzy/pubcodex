<script lang="ts">
  interface Film {
    id: number;
    title: string;
    rating: number;
  }

  let films: Film[] = [
    { id: 1, title: 'The Matrix', rating: 0 },
    { id: 2, title: 'Inception', rating: 0 },
    { id: 3, title: 'The Godfather', rating: 0 }
  ];

  function setRating(film: Film, rating: number) {
    film.rating = rating;
  }
</script>

<h1>rainbowsniffer - Film Ratings</h1>
<ul>
  {#each films as film}
    <li>
      <span>{film.title}</span>
      {#each Array(5) as _, i}
        <button on:click={() => setRating(film, i + 1)}>
          {i < film.rating ? '★' : '☆'}
        </button>
      {/each}
    </li>
  {/each}
</ul>
