import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getPublicBoard } from '../api/board';
import { ItemCard } from '../components/ItemCard';
import { CAMPUS_AREAS, CATEGORY_LABELS, ITEM_CATEGORIES, type ItemCategory } from '../types/common';
import styles from './BoardPage.module.css';

const PAGE_SIZE = 12;

export function PublicBoardPage() {
  const [category, setCategory] = useState<ItemCategory | ''>('');
  const [area, setArea] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [queryText, setQueryText] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['public-board', category, area, queryText, page],
    queryFn: () =>
      getPublicBoard({
        category,
        area,
        q: queryText,
        page,
        pageSize: PAGE_SIZE,
      }),
  });

  const handleSearchSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPage(1);
    setQueryText(searchInput.trim());
  };

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

  return (
    <section aria-labelledby="board-heading">
      <div className={styles.header}>
        <div>
          <h1 id="board-heading">Public Lost &amp; Found Board</h1>
          <p className="field-hint">
            Only public item descriptions and location-free photos are shown here.
          </p>
        </div>
        <Link className="btn btn-primary" to="/report-lost">
          Report a Lost Item
        </Link>
      </div>

      <form className={styles.filters} onSubmit={handleSearchSubmit} aria-label="Filter items">
        <div className="field-group">
          <label className="field-label" htmlFor="filter-category">
            Category
          </label>
          <select
            id="filter-category"
            className="field-select"
            value={category}
            onChange={(event) => {
              setCategory(event.target.value as ItemCategory | '');
              setPage(1);
            }}
          >
            <option value="">All categories</option>
            {ITEM_CATEGORIES.map((item) => (
              <option key={item} value={item}>
                {CATEGORY_LABELS[item]}
              </option>
            ))}
          </select>
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="filter-area">
            Campus area
          </label>
          <select
            id="filter-area"
            className="field-select"
            value={area}
            onChange={(event) => {
              setArea(event.target.value);
              setPage(1);
            }}
          >
            <option value="">All areas</option>
            {CAMPUS_AREAS.map((campusArea) => (
              <option key={campusArea} value={campusArea}>
                {campusArea}
              </option>
            ))}
          </select>
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="filter-query">
            Search keywords
          </label>
          <input
            id="filter-query"
            type="search"
            className="field-input"
            placeholder="e.g. black backpack"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
        </div>

        <div className="field-group" style={{ justifyContent: 'flex-end' }}>
          <button type="submit" className="btn btn-secondary">
            Apply filters
          </button>
        </div>
      </form>

      {isLoading ? (
        <p role="status">Loading board items...</p>
      ) : isError ? (
        <p className="alert-error" role="alert">
          {error instanceof Error ? error.message : 'Unable to load the public board.'}
        </p>
      ) : !data || data.items.length === 0 ? (
        <p className="alert-info">No lost items match the current filters.</p>
      ) : (
        <>
          <div className={styles.grid}>
            {data.items.map((item) => (
              <ItemCard key={item.id} report={item} />
            ))}
          </div>

          <nav className={styles.pagination} aria-label="Pagination">
            <button
              type="button"
              className="btn btn-secondary"
              disabled={page <= 1}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              Previous
            </button>
            <span>
              Page {data.page} of {totalPages} ({data.totalCount} items)
            </span>
            <button
              type="button"
              className="btn btn-secondary"
              disabled={page >= totalPages}
              onClick={() => setPage((current) => current + 1)}
            >
              Next
            </button>
          </nav>
        </>
      )}
    </section>
  );
}
