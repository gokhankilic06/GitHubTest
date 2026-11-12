import { apiClient } from './client';
import type { Dosya, DosyaFormVerisi, DosyaOzeti } from '../types';

export async function dosyalariListele(arama?: string, durumFiltre?: string): Promise<DosyaOzeti[]> {
  const res = await apiClient.get<DosyaOzeti[]>('/api/dosyalar', {
    params: { arama: arama || undefined, durumFiltre: durumFiltre || undefined },
  });
  return res.data;
}

export async function dosyaDetayiniGetir(id: number): Promise<Dosya> {
  const res = await apiClient.get<Dosya>(`/api/dosyalar/${id}`);
  return res.data;
}

export async function dosyaOlustur(form: DosyaFormVerisi): Promise<Dosya> {
  const res = await apiClient.post<Dosya>('/api/dosyalar', form);
  return res.data;
}

export async function dosyaGuncelle(id: number, form: DosyaFormVerisi): Promise<Dosya> {
  const res = await apiClient.put<Dosya>(`/api/dosyalar/${id}`, form);
  return res.data;
}
