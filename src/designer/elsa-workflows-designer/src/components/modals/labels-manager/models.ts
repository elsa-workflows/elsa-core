export interface CreateLabelEventArgs {
  name: string;
  description?: string;
  color?: string;
}

export interface UpdateLabelEventArgs {
  id: string;
  name: string;
  description?: string;
  color?: string;
}

export interface DeleteLabelEventArgs {
  id: string;
}
