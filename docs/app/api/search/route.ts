import { source } from '@/lib/source';
import { createFromSource } from 'fumadocs-core/search/server';

// Index de recherche en français (racinisation et mots vides adaptés).
export const { GET } = createFromSource(source, { language: 'french' });
