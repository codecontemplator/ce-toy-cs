namespace ce_toy_cs
{
    // ref: https://www.enterprise-software-development.eu/posts/2019/11/19/option-type.html
    public struct Option<T>
    {
        public static Option<T> None => default;
        public static Option<T> Some(T value) => new Option<T>(value);

        public readonly bool isSome;
        public readonly T value;

        Option(T value)
        {
            this.value = value;
            isSome = this.value is { };
        }

        public bool IsSome(out T value)
        {
            value = this.value;
            return isSome;
        }

        public override string ToString()
        {
            return isSome ? value.ToString() : "<<null>>";
        }
    }
}
