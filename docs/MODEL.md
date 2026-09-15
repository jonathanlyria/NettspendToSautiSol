# Artist graph model

## Data and objective

Each vertex represents an artist identified by a Spotify ID. Each stored connection contains a Last.fm similarity value `s` in `[0, 1]`. Higher means more similar, according to the [Last.fm API definition](https://www.last.fm/api/show/artist.getSimilar).

At graph-loading time, similarity becomes a non-negative traversal cost:

`c(u, v) = 1 - s(u, v)`

For a route `P`, the objective is to minimise `C(P) = sum(c(u, v) for each edge in P)`. Dijkstra's algorithm is applicable because these costs are non-negative. The database retains the raw similarity scores so alternative objectives can be evaluated later.

This choice was adopted on 15 September 2026. The previous implementation minimised the sum of raw similarities, which could prefer weak connections. The new objective favours routes whose total dissimilarity is smaller.

## Interpretation

Two steps of similarity 0.9 have total dissimilarity 0.2. One step of similarity 0.2 has dissimilarity 0.8. The two-step route wins. There is no explicit maximum route length or separate per-hop penalty; adding either would define a different objective.

A valid route begins at the requested source and ends at the destination. If an endpoint is missing or disconnected, the algorithm returns no route and the API responds with 404. A known isolated artist has a valid self-route. Invalid, negative, non-finite, or above-one similarity inputs are rejected.

## Assumptions and limitations

- **Similarity is a score, not a probability.** A small route cost is not a confidence interval or a prediction of listener enjoyment.
- **Undirected edges are an approximation.** The loader makes each stored connection traversable in both directions. Direction-dependent similarities are not modelled separately.
- **Coverage is selective.** The expander filters artists using Spotify popularity thresholds, release dates, and matching top-track names. This can exclude less popular artists and favour particular parts of the catalogue.
- **The snapshot has no collection timestamp per observation.** It should not be described as a current view of either provider.
- **Duplicate or conflicting observations need a policy.** The existing database schema permits duplicate connection rows. Loading repeated endpoints overwrites the earlier loaded value; a future ingestion revision should make deduplication and aggregation explicit.
- **The model optimises artist transitions.** Track ordering and listening quality are separate problems.
- **The expander's retries and progress marking need further work.** Failed or partial external responses can affect graph coverage. This cleanup does not claim to have solved ingestion reliability.

## Verification

`tests/Pathfinding.Checks` uses the production classes. Its database test creates a temporary SQLite graph to check score conversion, retained raw data, and isolated artists. Its generated-graph check compares path costs with Floyd–Warshall, an independent all-pairs shortest-path method, using a fixed random seed.

These tests establish behaviour on those cases. They do not validate the quality of the input data or prove that users prefer the resulting playlists.

## Research directions

Evaluate a fixed set of source–destination pairs against:

1. Unweighted shortest paths, to establish a minimum-hop baseline.
2. Total dissimilarity, the current objective.
3. A negative-log similarity cost, where positive similarities permit it; this optimises a product of scores but does not turn them into probabilities.
4. Dissimilarity with an explicit per-hop penalty or length constraint.

Report route length, coverage, repeated-artist handling, runtime, and a separately defined listening-quality assessment. Keep the evaluation pairs fixed when comparing methods, document data provenance, and show failures as well as successful examples.
