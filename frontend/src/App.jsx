import { useState } from 'react';
import './App.css';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const DEMO_QUERIES = [
  "the cat in the hat",
  "cat in le hut",
  "there and back again",
  "hairy plopper",
  "true crime",
  "a book about a kid who flew from planet to planet in space and stuff. was he german or something?",
  "lord of the rings fellowship",
  "book about a wizard school",
  "nvje vjnsrijvnsj;vnjseeeeeeefmjs ejems;jl f"
];

function App() {
  const [query, setQuery] = useState('');
  const [model, setModel] = useState(0); // 0=GeminiFlashLite, 1=GeminiFlash, 2=Gpt4oMini
  const [temperature, setTemperature] = useState(0.7);
  const [results, setResults] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const searchBooks = async (searchQuery) => {
    setLoading(true);
    setError(null);
    setResults(null);

    try {
      const response = await fetch(
        `${API_URL}/api/bookMatch/match?query=${encodeURIComponent(searchQuery)}&model=${model}`
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        const errorMessage = errorData?.message || response.statusText;
        throw new Error(`${response.status} - ${errorMessage}`);
      }

      const data = await response.json();
      setResults(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (query.trim()) {
      searchBooks(query);
    }
  };

  const runDemo = (demoQuery) => {
    setQuery(demoQuery);
    searchBooks(demoQuery);
  };

  return (
    <div className="app">
      <header>
        <h1>BookMatcher</h1>
      </header>

      <main>
        <form onSubmit={handleSubmit}>
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Enter a messy book description..."
            disabled={loading}
          />
          <select
            value={model}
            onChange={(e) => setModel(Number(e.target.value))}
            disabled={loading}
          >
            <option value={0}>Gemini Flash Lite</option>
            <option value={1}>Gemini Flash</option>
            <option value={2}>GPT-4o Mini</option>
          </select>
          <button type="submit" disabled={loading || !query.trim()}>
            {loading ? 'Searching...' : 'Search'}
          </button>
        </form>

        <div className="temperature-control">
          <label htmlFor="temperature">
            Temperature: {temperature.toFixed(1)}
          </label>
          <input
            id="temperature"
            type="range"
            min="0"
            max="1"
            step="0.1"
            value={temperature}
            onChange={(e) => setTemperature(parseFloat(e.target.value))}
            disabled={loading}
          />
          <div className="temperature-labels">
            <span>Precise</span>
            <span>Creative</span>
          </div>
        </div>

        <div className="demo-buttons">
          <p><strong>Try a demo query:</strong></p>
          {DEMO_QUERIES.map((dq, idx) => (
            <button
              key={idx}
              onClick={() => runDemo(dq)}
              disabled={loading}
              className="demo-btn"
            >
              "{dq}"
            </button>
          ))}
        </div>

        {error && (
          <div className="error">
            <h3>Error</h3>
            <p>{error}</p>
          </div>
        )}

        {results && (
          <div className="results">
            <h2>Results ({results.matches?.length || 0} matches)</h2>
            {results.matches && results.matches.length > 0 ? (
              <div className="matches">
                {results.matches.map((match, idx) => (
                  <div key={idx} className="match-card">
                    <div className="match-header">
                      <h3>{match.title}</h3>
                      {match.first_publish_year && (
                        <span className="year">({match.first_publish_year})</span>
                      )}
                    </div>

                    <div className="match-authors">
                      <strong>Authors:</strong>{' '}
                      {match.primary_authors?.join(', ') || 'Unknown'}
                      {match.contributors && match.contributors.length > 0 && (
                        <span className="contributors">
                          {' '}• Contributors: {match.contributors.join(', ')}
                        </span>
                      )}
                    </div>

                    <p className="explanation">{match.explanation}</p>

                    <div className="match-links">
                      {match.cover_url && (
                        <img src={match.cover_url} alt={match.title} className="cover" />
                      )}
                      <a
                        href={match.openlibrary_url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="ol-link"
                      >
                        View on OpenLibrary →
                      </a>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p>No matches found. Try a different query!</p>
            )}
          </div>
        )}
      </main>
    </div>
  );
}

export default App;
