import { apiClient } from './client';
import type { KayitFormuVerisi, YeniDosyaOnerileri } from '../types';

export async function kayitFormuVerileriniGetir(): Promise<KayitFormuVerisi> {
  const res = await apiClient.get<KayitFormuVerisi>('/api/referans/kayit-formu');
  return res.data;
}

export async function yeniDosyaOnerileriniGetir(): Promise<YeniDosyaOnerileri> {
  const res = await apiClient.get<YeniDosyaOnerileri>('/api/referans/yeni-dosya-onerileri');
  return res.data;
}
